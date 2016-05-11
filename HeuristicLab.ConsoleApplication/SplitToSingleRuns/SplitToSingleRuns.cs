using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HeuristicLab.Common;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class SplitToSingleRuns : IRunStrategy {

    public void Start(Options options) {
      foreach (var filePath in options.InputFiles) {
        RunFile(filePath, options.Repetitions, options.Verbose);
      }
    }

    private void RunFile(string filePath, int repetitions, bool verbose) {
      string fileName = Path.GetFileName(filePath);
      List<HLRunInfo> tasks = new List<HLRunInfo>();
      var optimizer = Load(filePath);
      optimizer.Prepare();
      optimizer.Runs.Clear();
      if (optimizer == null) { Console.WriteLine(String.Format("{0} does not contain an optimizer.", filePath)); return; }
      var listOfOptimizer = UnrollOptimizer(optimizer);

      for (int i = 0; i < repetitions; i++) {
        foreach (var opt in listOfOptimizer) {

          int coresRequired = opt as EngineAlgorithm != null && (opt as EngineAlgorithm).Engine as HeuristicLab.ParallelEngine.ParallelEngine != null
                              ? ((opt as EngineAlgorithm).Engine as HeuristicLab.ParallelEngine.ParallelEngine).DegreeOfParallelism
                              : 1;
          coresRequired = coresRequired > 0 ? coresRequired : Environment.ProcessorCount;

          string savePath = Path.GetTempFileName();
          Helper.printToConsole(String.Format("Temporary save path: {0}", savePath), fileName);
          tasks.Add(new HLRunInfo((IOptimizer)opt.Clone(), filePath, coresRequired, savePath));
        }
      }

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
          }
          catch (Exception e) {
            Helper.printToConsole(e, "Thread Exception.");
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
      RunCollection allRuns = new RunCollection();

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

      ContentManager.Save(allRuns, Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "-Results.hl"), true);
    }

    private IOptimizer Load(string filePath) {
      Helper.printToConsole("Loading...", Path.GetFileName(filePath));
      var content = ContentManager.Load(filePath);

      Helper.printToConsole("Loading completed!", Path.GetFileName(filePath));
      Helper.printToConsole("Content loaded: " + content.ToString(), Path.GetFileName(filePath));

      return content as IOptimizer;
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
