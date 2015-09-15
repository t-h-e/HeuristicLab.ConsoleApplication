using System;
using System.Threading;
using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class Program {

    private static ManualResetEventSlim eventHandle = new ManualResetEventSlim(false, 1);
    public static void Main(string[] args) {
      try {
        var options = new Options();
        if (CommandLine.Parser.Default.ParseArguments(args, options)) {
          if (options.Verbose) {
            Console.WriteLine("Filename: {0}", options.InputFile);
            Console.WriteLine("Repetitions: {0}", options.Repetitions);
          }

          Program p = new Program(options.InputFile, options.Repetitions, options.Verbose);
          if (!p.Load()) {
            Console.WriteLine("File could not be loaded.");
            return;
          }
          for (int i = 0; i < options.Repetitions; i++) {
            Console.WriteLine("rep: " + i);
            p.Start();
            eventHandle.Wait();
            eventHandle.Reset();
            p.Save();
          }
        } else {
          Console.WriteLine(options.GetUsage());
        }
      }
      catch (Exception e) {
        Console.WriteLine(e.Message);
        Console.WriteLine(e.StackTrace);
      }
    }

    private IStorableContent content;
    private IOptimizer optimizer;
    private int numberOfRuns;
    private string filePath;
    private int initialNumberOfRuns;
    private int repetitons;
    private bool verbose;
    private bool loaded = false;

    public Program(string filePath, int repetitons, bool verbose = false) {
      this.filePath = filePath;
      this.repetitons = repetitons;
      this.verbose = verbose;

      Console.Clear();
    }

    public bool Load() {
      if (loaded) {
        Console.WriteLine("A file has already been loaded.");
        return false;
      }
      ContentManager.Initialize(new PersistenceContentManager());
      content = ContentManager.Load(filePath);

      Console.WriteLine("Loading completed!");
      Console.WriteLine("Content loaded: " + content.ToString());

      optimizer = content as IOptimizer;
      if (optimizer != null) {
        numberOfRuns = NumberOfRuns(optimizer);
        initialNumberOfRuns = optimizer.Runs.Count;
        Console.WriteLine(String.Format("Initial number of runs: {0}", initialNumberOfRuns));
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
        Console.WriteLine("Nothing has been loaded. Call Load() before Start().");
        return;
      }
      if (optimizer.ExecutionState != ExecutionState.Started) {
        optimizer.Prepare();
        optimizer.Start();
      }
    }

    public void Save() {
      if (content != null) {
        Console.WriteLine("Saving...");
        ContentManager.Save(content, filePath, false);
        Console.WriteLine("Saved");
      } else {
        Console.WriteLine("No content available");
      }

    }

    private TimeSpan diffHour = new TimeSpan(1, 0, 0); // one hour
    private TimeSpan diffMinute = new TimeSpan(0, 1, 0); // one minute
    private TimeSpan last = TimeSpan.Zero;

    private void Optimizer_ExecutionTimeChanged(object sender, EventArgs e) {
      if ((verbose && optimizer.ExecutionTime.Subtract(last) > diffMinute)
        || (!verbose && optimizer.ExecutionTime.Subtract(last) > diffHour)) {
        //Console.SetCursorPosition(0, 2);
        Console.WriteLine(optimizer.ExecutionTime);

        last = optimizer.ExecutionTime;
      }
    }

    private void Optimizer_Runs_RowsChanged(object sender, EventArgs e) {
      PrintRuns();
    }

    private void PrintRuns() {
      //Console.SetCursorPosition(0, 3);
      Console.WriteLine(String.Format("Number of Runs: {0}/{1}; Initial number of runs: {2}", optimizer.Runs.Count - initialNumberOfRuns, numberOfRuns * repetitons, initialNumberOfRuns));
    }

    private void Optimizer_ExecutionStateChanged(object sender, EventArgs e) {
      //Console.SetCursorPosition(0, 4);
      Console.WriteLine(optimizer.ExecutionState);
    }

    private void Optimizer_Stopped(object sender, EventArgs e) {
      //Console.SetCursorPosition(0, 5);
      Console.WriteLine("Finished.");
      eventHandle.Set();
    }

    private void Optimizer_Exception(object sender, EventArgs<Exception> e) {
      //Console.SetCursorPosition(0, 6);
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
