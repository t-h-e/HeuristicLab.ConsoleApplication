using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HeuristicLab.Common;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class SplitToSingleRuns : IRunStrategy {

    private string filePath;
    private string fileName;
    private bool verbose;

    public void Start(Options options) {
      int count = options.InputFiles.Count;
      if (count > 1) { throw new ArgumentException("SplitToSingleRuns currently supports only a single file."); }

      this.verbose = options.Verbose;
      this.filePath = options.InputFiles.First();
      this.fileName = Path.GetFileName(filePath);

      var optimizer = Load(filePath);
      if (optimizer == null) { throw new ArgumentException("File does not contain an optimizer."); }

      optimizer.Prepare();
      optimizer.Runs.Clear();

      var tasks = UnrollOptimizer(optimizer);
      int taskCount = tasks.Count();

      ConcurrentBag<IRun> results = new ConcurrentBag<IRun>();

      ParallelOptions parallelOptions = new ParallelOptions();
      parallelOptions.MaxDegreeOfParallelism = 2;

      Parallel.ForEach<HLTask>(tasks, parallelOptions, (hl) => {
        hl.Start();
        results.Add(hl.GetRun());
        Helper.printToConsole(String.Format("Number of Runs: {0}/{1}", taskCount, results.Count), fileName);
      });

      RunCollection runs = new RunCollection(results);
      ContentManager.Save(runs, Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + "-Results.hl"), false);

    }

    public IOptimizer Load(string filePath) {
      Helper.printToConsole("Loading...", fileName);
      var content = ContentManager.Load(filePath);

      Helper.printToConsole("Loading completed!", fileName);
      Helper.printToConsole("Content loaded: " + content.ToString(), fileName);

      return content as IOptimizer;
    }

    private static long count = 0;

    public IEnumerable<HLTask> UnrollOptimizer(IOptimizer optimizer) {
      List<HLTask> tasks = new List<HLTask>();

      var batchRun = optimizer as BatchRun;
      var experiment = optimizer as Experiment;

      if (batchRun != null && batchRun.Optimizer != null) {
        for (int i = 0; i < batchRun.Repetitions; i++) {
          tasks.AddRange(UnrollOptimizer(batchRun.Optimizer));
        }
      } else if (experiment != null) {
        foreach (var opt in experiment.Optimizers) {
          tasks.AddRange(UnrollOptimizer(opt));
        }
      } else {
        tasks.Add(new HLTask(optimizer, fileName + count++, verbose));
      }

      return tasks;
    }
  }
}
