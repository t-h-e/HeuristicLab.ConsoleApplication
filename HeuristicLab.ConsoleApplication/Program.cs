using System;
using System.IO;
using System.Threading;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class Program {

    private static ManualResetEventSlim eventHandle = new ManualResetEventSlim(false, 1);
    public static void Main(string[] args) {
      if (args.Length < 1 || !File.Exists(args[0])) {
        Console.WriteLine("First argument has to be a file path");
        return;
      }
      string filePath = args[0];
      Program p = new Program(filePath);
      p.Start();
      eventHandle.Wait();
      p.SaveAndExit();
    }
    private IStorableContent content;
    private IOptimizer optimizer;
    private int numberOfRuns;
    private string filePath;
    public Program(string filePath) {
      this.filePath = filePath;
    }

    public void Start() {
      ContentManager.Initialize(new PersistenceContentManager());
      content = ContentManager.Load(filePath);

      Console.WriteLine("Loading completed!");
      Console.WriteLine("Content loaded: " + content.ToString());

      optimizer = content as IOptimizer;
      if (optimizer != null) {
        numberOfRuns = NumberOfRuns(optimizer);
        PrintRuns();

        optimizer.ExceptionOccurred += new EventHandler<EventArgs<Exception>>(Optimizer_Exception);
        optimizer.ExecutionStateChanged += new EventHandler(Optimizer_ExecutionStateChanged);
        optimizer.Stopped += new EventHandler(Optimizer_Stopped);
        optimizer.ExecutionTimeChanged += new EventHandler(Optimizer_ExecutionTimeChanged);
        optimizer.Runs.UpdateOfRunsInProgressChanged += new EventHandler(Optimizer_Runs_UpdateOfRunsInProgressChanged);

        optimizer.Prepare();
        optimizer.Start();
      } else {
        Console.WriteLine("Unknown content in file: " + content.ToString());
      }
    }

    public void SaveAndExit() {
      if (content != null) {
        Console.WriteLine("Saving...");
        ContentManager.Save(content, filePath, false);
        Console.WriteLine("Saved");
      } else {
        Console.WriteLine("No content available");
      }

    }

    private void Optimizer_ExecutionTimeChanged(object sender, EventArgs e) {
      Console.SetCursorPosition(0, 2);
      Console.WriteLine(optimizer.ExecutionTime);
    }

    private void Optimizer_Runs_UpdateOfRunsInProgressChanged(object sender, EventArgs e) {
      PrintRuns();
    }

    private void PrintRuns() {
      Console.SetCursorPosition(0, 3);
      Console.WriteLine(String.Format("Runs: {0}/{1}", optimizer.Runs.Count, numberOfRuns));
    }

    private void Optimizer_ExecutionStateChanged(object sender, EventArgs e) {
      Console.SetCursorPosition(0, 4);
      Console.WriteLine(optimizer.ExecutionState);
    }

    private void Optimizer_Stopped(object sender, EventArgs e) {
      Console.SetCursorPosition(0, 5);
      Console.WriteLine("Finished.");
      eventHandle.Set();
    }

    private void Optimizer_Exception(object sender, EventArgs<Exception> e) {
      Console.SetCursorPosition(0, 6);
      Console.WriteLine("Optimizer Exception");
      Console.WriteLine(e.Value.Message);
      Console.WriteLine(e.Value.StackTrace);
    }

    private int NumberOfRuns(IOptimizer optimizer) {
      var batchRun = optimizer as BatchRun;
      var experiment = optimizer as Experiment;

      if (batchRun != null && batchRun.Optimizer != null) {
        return batchRun.Repetitions * NumberOfRuns(batchRun.Optimizer);
      } else if (experiment != null) {
        int runs = 0;
        foreach (var opt in experiment.Optimizers) {
          runs += NumberOfRuns(opt);
        }
        return runs;
      } else { return 1; }
    }
  }
}
