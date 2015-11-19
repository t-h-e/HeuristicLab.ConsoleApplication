﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using HeuristicLab.Common;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class HeuristicLabWorkingThread {

    private ManualResetEventSlim saveEventHandle = new ManualResetEventSlim(false, 1);

    private IStorableContent content;
    private IOptimizer optimizer;
    private int numberOfRuns;
    private string filePath;
    private string fileName;
    private int initialNumberOfRuns;
    private int repetitions;
    private bool verbose;
    private bool loaded = false;
    private ManualResetEventSlim finishedEvent;

    private TimeSpan lastTimespan = TimeSpan.Zero;

    public HeuristicLabWorkingThread(string filePath, int repetitons, ManualResetEventSlim finishedEvent, bool verbose = false) {
      this.filePath = filePath;
      this.fileName = Path.GetFileName(filePath);
      this.repetitions = repetitons;
      this.finishedEvent = finishedEvent;
      this.verbose = verbose;
    }

    public void Run() {
      if (!Load()) {
        printToConsole("File could not be loaded.");
        finishedEvent.Set();
        return;
      }
      for (int i = 0; i < repetitions; i++) {
        Start();
        saveEventHandle.Wait();
        saveEventHandle.Reset();
        Save();
      }

      printToConsole("Finished");
      finishedEvent.Set();
    }

    public bool Load() {
      if (loaded) {
        printToConsole("A file has already been loaded.");
        return false;
      }
      content = ContentManager.Load(filePath);

      printToConsole("Loading completed!");
      printToConsole("Content loaded: " + content.ToString());

      optimizer = content as IOptimizer;
      if (optimizer != null) {
        numberOfRuns = NumberOfRuns(optimizer);
        initialNumberOfRuns = optimizer.Runs.Count;
        printToConsole(String.Format("Initial number of runs: {0}", initialNumberOfRuns));
        PrintRuns();

        optimizer.ExceptionOccurred += new EventHandler<EventArgs<Exception>>(Optimizer_Exception);
        optimizer.ExecutionStateChanged += new EventHandler(Optimizer_ExecutionStateChanged);
        optimizer.Stopped += new EventHandler(Optimizer_Stopped);
        optimizer.ExecutionTimeChanged += new EventHandler(Optimizer_ExecutionTimeChanged);
        optimizer.Runs.RowsChanged += new EventHandler(Optimizer_Runs_RowsChanged);
      }
      loaded = optimizer != null;
      return loaded;
    }

    public void Start() {
      if (!loaded) {
        printToConsole("Nothing has been loaded. Call Load() before Start().");
        return;
      }
      optimizer.Prepare();
      lastTimespan = TimeSpan.Zero;
      optimizer.Start();
    }

    public void Save() {
      if (content != null) {
        printToConsole("Saving...");
        ContentManager.Save(content, filePath, false);
        printToConsole("Saved");
      } else {
        printToConsole("No content available");
      }

    }

    private TimeSpan diffHour = new TimeSpan(1, 0, 0); // one hour
    private TimeSpan diffMinute = new TimeSpan(0, 1, 0); // one minute

    private void Optimizer_ExecutionTimeChanged(object sender, EventArgs e) {
      if ((verbose && optimizer.ExecutionTime.Subtract(lastTimespan) > diffMinute)
        || (!verbose && optimizer.ExecutionTime.Subtract(lastTimespan) > diffHour)) {
        printToConsole(optimizer.ExecutionTime + "; " + GetGeneration(optimizer));
        lastTimespan = optimizer.ExecutionTime;
      }
    }

    private string GetGeneration(IOptimizer opt) {
      var engineAlgorithm = opt.NestedOptimizers.Where(o => o is EngineAlgorithm
        && o.ExecutionState.Equals(HeuristicLab.Core.ExecutionState.Started)).FirstOrDefault() as EngineAlgorithm;

      if (engineAlgorithm != null && engineAlgorithm.Results.ContainsKey("Generations")) {
        return engineAlgorithm.Results["Generations"].ToString();
      }

      return "No generation info found.";
    }

    private void Optimizer_Runs_RowsChanged(object sender, EventArgs e) {
      PrintRuns();
    }

    private void PrintRuns() {
      printToConsole(String.Format("Number of Runs: {0}/{1}", optimizer.Runs.Count - initialNumberOfRuns, numberOfRuns * repetitions));
    }

    private void Optimizer_ExecutionStateChanged(object sender, EventArgs e) {
      printToConsole(optimizer.ExecutionState);
    }

    private void Optimizer_Stopped(object sender, EventArgs e) {
      saveEventHandle.Set();
    }

    private void Optimizer_Exception(object sender, EventArgs<Exception> e) {
      printToConsole("Optimizer Exception");
      printToConsole(e.Value.Message);
      printToConsole(e.Value.StackTrace);
    }

    private void printToConsole(object value) {
      printToConsole(value.ToString());
    }

    private void printToConsole(string value) {
      StringBuilder strBuilder = new StringBuilder(DateTime.Now.ToString());
      strBuilder.Append(" ");
      strBuilder.Append(fileName);
      strBuilder.Append(": ");
      strBuilder.Append(value);
      Console.WriteLine(strBuilder.ToString());
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
