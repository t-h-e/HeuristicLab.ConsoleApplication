using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
      foreach (var filePath in options.InputFiles) {
        RunFile(filePath, options.Repetitions, options.StartSeed, options.Parallelism, options.Verbose);
      }
    }

    private void RunFile(string filePath, int repetitions, int startSeed, int parallelism, bool verbose) {
      string fileName = Path.GetFileName(filePath);
      List<HLRunInfo> tasks = new List<HLRunInfo>();
      var optimizer = Load<IOptimizer>(filePath);
      optimizer.Prepare();
      optimizer.Runs.Clear();
      if (optimizer == null) { Console.WriteLine(String.Format("{0} does not contain an optimizer.", filePath)); return; }
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
          Helper.printToConsole(String.Format("Temporary save path: {0}", savePath), fileName);
          tasks.Add(new HLRunInfo(++count, clone, filePath, coresRequired, savePath));
        }
      }

      SetSeeds(tasks, startSeed, fileName);

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
  }
}
