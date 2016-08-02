using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Data;
using HeuristicLab.Misc;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class SplitToSingleRuns : IRunStrategy {

    public void Start(Options options) {
      if (options.Continue) {
        Console.WriteLine("ATTENTION!!! -c flag should only be used for experiments with a single algorithm setting in each file. It is recommended to redo the whole experiment");

        options = ContinueRuns(options.InputFiles.First());
        Console.WriteLine("Continue normally.");
      }

      for (int i = 0; i < options.InputFiles.Count; i++) {
        Console.WriteLine(String.Format("File{{0}}: {1}", i, options.InputFiles[i]));
      }
      foreach (var filePath in options.InputFiles) {
        RunFile(filePath, options.Repetitions, options.StartSeed, options.Parallelism, options.Verbose);
      }
    }

    private static string COMMANDSUSED = "Used command: ";
    private const string LOADINGCOMPLETE = "Loading completed!";
    private const string CONTENTLOADED = "Content loaded: ";
    private static string FINISHEDSAVING = "Finished Saving";
    private const string SAVEPATH = "Temporary save path: ";

    private static string fileNumberPattern = @"^File{(?<number>[0-9]+)}:\s(?<filename>.*)$";
    private static Regex fileNumberRegex = new Regex(fileNumberPattern);

    private static string fileNamePattern = @"^[0-9]+\/[0-9]+\/[0-9]+\s[0-9]+:[0-9]+:[0-9]+\s(?<filename>.*?): Loading completed!";
    private static Regex fileNameRegex = new Regex(fileNamePattern);

    private Options ContinueRuns(string filePath) {
      string fileName = Path.GetFileNameWithoutExtension(filePath);
      string[] lines = File.ReadAllLines(filePath);

      int pos = 0;
      while (!lines[pos].Contains(COMMANDSUSED) && pos < lines.Length) {
        pos++;
      }
      var options = new Options();
      if (!CommandLine.Parser.Default.ParseArguments(lines[pos].Substring(COMMANDSUSED.Length).Split(), options)) {
        Helper.printToConsole("Could not load parameters used", fileName);
      }

      pos++;
      // sanity check that files are in the correct order
      List<string> files = new List<string>();
      while (fileNumberRegex.IsMatch(lines[pos]) && pos < lines.Length) {
        var match = fileNumberRegex.Match(lines[pos]);
        if (files.Count == int.Parse(match.Groups["number"].Value)) {
          files.Add(match.Groups["filename"].Value);
        }
        pos++;
      }
      if (!files.SequenceEqual(options.InputFiles)) {
        options.InputFiles = files;
      }

      pos = lines.Length - 1;
      while (!lines[pos].Contains(LOADINGCOMPLETE) && pos >= 0) {
        pos--;
      }

      if (pos < 0 || !fileNameRegex.IsMatch(lines[pos])) {
        Console.WriteLine("Could not find a successfully loaded file.");
        return options;
      }

      string lastFileExecuted = fileNameRegex.Match(lines[pos]).Groups["filename"].Value;
      string filePathOfLastExecutedFile = options.InputFiles.Where(x => x.EndsWith(lastFileExecuted)).First();
      int indexOfIncompleteFile = options.InputFiles.IndexOf(filePathOfLastExecutedFile);

      options.InputFiles = options.InputFiles.Skip(indexOfIncompleteFile + 1).ToList(); // also skip the current one as it will he handled now

      pos += 2;
      int pathPos = -1;
      List<string> savePaths = new List<string>();
      while (pos < lines.Length && (pathPos = lines[pos].IndexOf(SAVEPATH)) >= 0) {
        pathPos += SAVEPATH.Length;
        savePaths.Add(lines[pos].Substring(pathPos, lines[pos].Length - pathPos));
        pos++;
      }

      Helper.printToConsole("Generate Tasks for last executed file...", fileName);
      List<HLRunInfo> tasks = GenerateTasks(filePathOfLastExecutedFile, options.Repetitions, options.Parallelism, false);
      List<HLRunInfo> ToDo = new List<HLRunInfo>();

      if (tasks.Count != savePaths.Count) { throw new ArgumentException("number of runs is not the same as number of save paths"); }

      // now check which files are not finished
      Helper.printToConsole("Check last Experiment...", fileName);
      RunCollection collectedRuns = new RunCollection();
      if (options.StartSeed < 0) {
        for (int i = 0; i < savePaths.Count; i++) {
          tasks[i].SavePath = savePaths[i];
          if (!File.Exists(savePaths[i]) || new FileInfo(savePaths[i]).Length <= 0) {
            ToDo.Add(tasks[i]);
          }
        }
      } else {
        List<int> seeds = Enumerable.Range(options.StartSeed, tasks.Count).ToList();
        int taskPos = 0;
        for (int i = 0; i < savePaths.Count; i++) {
          if (File.Exists(savePaths[i]) && new FileInfo(savePaths[i]).Length > 0) {
            RunCollection runCollection = null;
            try {
              runCollection = ContentManager.Load(savePaths[i]) as RunCollection;
            } catch (Exception) {
              Helper.printToConsole("Warning: Exception caught when loading a tmp file.", fileName);
              continue;
            }
            if (runCollection != null) {
              tasks[taskPos++].SavePath = savePaths[i];
              seeds.Remove(((IntValue)runCollection.First().Parameters["Seed"]).Value);
            }
          }
        }

        ToDo = tasks.Skip(tasks.Count - seeds.Count).ToList();
        SetSeeds(ToDo, seeds, fileName);
      }

      RunTasks(ToDo, options.Verbose, fileName);

      SaveRuns(tasks, filePathOfLastExecutedFile, fileName);

      return options;
    }

    private void RunFile(string filePath, int repetitions, int startSeed, int parallelism, bool verbose) {
      string fileName = Path.GetFileName(filePath);
      List<HLRunInfo> tasks = GenerateTasks(filePath, repetitions, parallelism, true);

      SetSeeds(tasks, startSeed, fileName);

      RunTasks(tasks, verbose, fileName);

      SaveRuns(tasks, filePath, fileName);
    }

    private List<HLRunInfo> GenerateTasks(string filePath, int repetitions, int parallelism, bool printTmpPaths) {
      string fileName = Path.GetFileName(filePath);
      List<HLRunInfo> tasks = new List<HLRunInfo>();
      var optimizer = Load<IOptimizer>(filePath);
      optimizer.Prepare();
      optimizer.Runs.Clear();
      if (optimizer == null) { throw new ArgumentException(String.Format("{0} does not contain an optimizer.", filePath)); }
      var listOfOptimizer = UnrollOptimizer(optimizer);

      int count = 0;
      for (int i = 0; i < repetitions; i++) {
        foreach (var opt in listOfOptimizer) {
          var clone = (IOptimizer)opt.Clone();
          int coresRequired = 1;
          if (parallelism != 0) {
            if (clone as EngineAlgorithm != null && (clone as EngineAlgorithm).Engine as ParallelEngine.ParallelEngine != null) {
              coresRequired = parallelism > 0 ? parallelism : Environment.ProcessorCount;
              ((clone as EngineAlgorithm).Engine as ParallelEngine.ParallelEngine).DegreeOfParallelism = coresRequired;
            }
          } else {
            coresRequired = clone as EngineAlgorithm != null && (clone as EngineAlgorithm).Engine as ParallelEngine.ParallelEngine != null
                                ? ((clone as EngineAlgorithm).Engine as ParallelEngine.ParallelEngine).DegreeOfParallelism
                                : 1;
            coresRequired = coresRequired > 0 ? coresRequired : Environment.ProcessorCount;
          }
          var algo = clone as Algorithm;
          if (algo != null) {
            var prob = algo.Problem as IParallelEvaluatorProblem;
            if (prob != null) {
              prob.DegreeOfParallelismParameter.Value.Value = coresRequired;
            }
          }

          string savePath = Path.GetTempFileName();
          if (printTmpPaths) { Helper.printToConsole(String.Format("Temporary save path: {0}", savePath), fileName); }
          tasks.Add(new HLRunInfo(++count, clone, filePath, coresRequired, savePath));
        }
      }
      return tasks;
    }

    private void SetSeeds(List<HLRunInfo> tasks, List<int> seeds, string filename) {
      if (seeds.Count != tasks.Count) {
        Helper.printToConsole("There are too many or too few seeds", filename);
        return;
      }
      var parameterizedNamedItems = tasks.Select(x => x.Optimizer).OfType<ParameterizedNamedItem>().ToList();
      if (parameterizedNamedItems.Count != tasks.Count) {
        throw new InvalidCastException("Cannot set seeds, because not all optimizer are of type ParameterizedNamedItem. Make sure that all optimizer are of type ParameterizedNamedItem or run without seting seeds or make sure.");
      }

      try {
        for (int i = 0; i < parameterizedNamedItems.Count; i++) {
          parameterizedNamedItems[i].GetType().InvokeMember("Seed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, Type.DefaultBinder, parameterizedNamedItems[i], new Object[] { new IntValue(seeds[i]) });
          parameterizedNamedItems[i].GetType().InvokeMember("SetSeedRandomly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, Type.DefaultBinder, parameterizedNamedItems[i], new Object[] { new BoolValue(false) });
        }
      } catch (Exception e) {
        Helper.printToConsole("One or more ParameterizedNamedItems do not have a property Seed or SetSeedRandomly.", filename);
        throw e;
      }
      Helper.printToConsole("Seeds have successfully been set", filename);
    }

    private void SetSeeds(List<HLRunInfo> tasks, int startSeed, string filename) {
      if (startSeed < 0) {
        Helper.printToConsole("No seeds are going to be set.", filename);
        return;
      }
      var parameterizedNamedItems = tasks.Select(x => x.Optimizer).OfType<ParameterizedNamedItem>();
      if (parameterizedNamedItems.Count() != tasks.Count) {
        throw new InvalidCastException("Cannot set seeds, because not all optimizer are of type ParameterizedNamedItem. Make sure that all optimizer are of type ParameterizedNamedItem or run without seting seeds or make sure.");
      }

      try {
        foreach (var item in parameterizedNamedItems) {
          item.GetType().InvokeMember("Seed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, Type.DefaultBinder, item, new Object[] { new IntValue(startSeed++) });
          item.GetType().InvokeMember("SetSeedRandomly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty, Type.DefaultBinder, item, new Object[] { new BoolValue(false) });
        }
      } catch (Exception e) {
        Helper.printToConsole("One or more ParameterizedNamedItems do not have a property Seed or SetSeedRandomly.", filename);
        throw e;
      }
      Helper.printToConsole("Seeds have successfully been set", filename);
    }

    private T Load<T>(string filePath) where T : class {
      Helper.printToConsole("Loading...", Path.GetFileName(filePath));
      var content = ContentManager.Load(filePath);

      Helper.printToConsole("Loading completed!", Path.GetFileName(filePath));
      Helper.printToConsole("Content loaded: " + content.ToString(), Path.GetFileName(filePath));

      return content as T;
    }

    public IEnumerable<IOptimizer> UnrollOptimizer(IOptimizer optimizer) {
      List<IOptimizer> optimizers = new List<IOptimizer>();

      var batchRun = optimizer as BatchRun;
      var experiment = optimizer as Experiment;

      if (batchRun != null && batchRun.Optimizer != null) {
        for (int i = 0; i < batchRun.Repetitions; i++) {
          optimizers.AddRange(UnrollOptimizer(batchRun.Optimizer));
        }
      } else if (experiment != null) {
        foreach (var opt in experiment.Optimizers) {
          optimizers.AddRange(UnrollOptimizer(opt));
        }
      } else {
        optimizers.Add(optimizer);
      }

      return optimizers;
    }

    private void RunTasks(List<HLRunInfo> tasks, bool verbose, string fileName) {
      SemaphoreSlim s = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

      TaskFactory tf = new TaskFactory();
      List<Task> waitForTasks = new List<Task>();

      int finished = 0;
      Helper.printToConsole(String.Format("Number of Runs: {0}/{1}", finished, tasks.Count), fileName);
      Object syncLock = new Object();

      int sleepCount = 0;  // prevents two threads from having the same seed due to being start at the same time, or at least makes it unlikely
      foreach (var task in tasks) {
        HLTask hl = new HLTask(task, verbose);
        for (int i = 0; i < task.CoresRequired; i++) {
          s.Wait();
        }

        waitForTasks.Add(tf.StartNew<bool>(() => {
          lock (syncLock) {
            Thread.Sleep(sleepCount);
            sleepCount++;
          }

          bool success = false;
          try {
            success = hl.Start();
          } catch (Exception e) {
            Helper.printToConsole(e, "Thread Exception.");
          }

          var algorithm = hl.Optimizer as IAlgorithm;
          if (algorithm != null && algorithm.Problem is IDisposable) {
            ((IDisposable)algorithm.Problem).Dispose();
          }
          s.Release(hl.runInfo.CoresRequired);
          lock (syncLock) {
            finished++;
            Helper.printToConsole(String.Format("Number of Runs: {0}/{1}", finished, tasks.Count), fileName);
          }
          return success;
        }));
      }

      Task.WaitAll(waitForTasks.ToArray());
    }

    private void SaveRuns(List<HLRunInfo> tasks, string filePath, string fileName) {
      Helper.printToConsole("Saving...", fileName);
      RunCollection allRuns;
      string saveFile = Path.GetFileNameWithoutExtension(filePath) + "-Results.hl";
      if (File.Exists(saveFile)) {
        allRuns = Load<RunCollection>(saveFile);
        if (allRuns == null) {
          Console.WriteLine(String.Format("{0} exists but it does not contain a RunCollection. File will be overwritten.", saveFile));
        }
      } else {
        allRuns = new RunCollection();
      }

      foreach (var task in tasks) {
        if (!File.Exists(task.SavePath)) {
          Helper.printToConsole(String.Format("WARNING: {0} does not exist. {0} has probably not been saved.", task.SavePath), fileName);
          continue;
        }
        var runCollection = ContentManager.Load(task.SavePath) as RunCollection;
        if (runCollection != null) {
          allRuns.AddRange(runCollection);
        } else {
          Helper.printToConsole(String.Format("WARNING: {0} is not a run collection.", task.SavePath), fileName);
        }
      }

      ContentManager.Save(allRuns, Path.Combine(Path.GetDirectoryName(filePath), saveFile), true);
      Helper.printToConsole(FINISHEDSAVING, fileName);
    }
  }
}
