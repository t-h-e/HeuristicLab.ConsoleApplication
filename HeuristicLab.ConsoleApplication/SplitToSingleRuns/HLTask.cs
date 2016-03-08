using System;
using System.Linq;
using System.Threading;
using HeuristicLab.Common;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class HLTask {

    private ManualResetEventSlim finishedEventHandle;

    private IOptimizer optimizer;

    private TimeSpan lastTimespan;

    private bool verbose;
    private string taskname;

    public HLTask(IOptimizer optimizer, string taskname, bool verbose) {
      this.optimizer = (IOptimizer)optimizer.Clone();

      this.taskname = taskname;
      this.verbose = verbose;

      RegisterEvents();
    }

    private void RegisterEvents() {
      optimizer.Stopped += new EventHandler(Optimizer_Stopped);
      optimizer.ExecutionTimeChanged += new EventHandler(Optimizer_ExecutionTimeChanged);
      optimizer.ExceptionOccurred += new EventHandler<EventArgs<Exception>>(Optimizer_Exception);
    }

    public void Start() {
      lastTimespan = TimeSpan.Zero;
      finishedEventHandle = new ManualResetEventSlim(false, 1);
      optimizer.Start();
      finishedEventHandle.Wait();
    }

    public IRun GetRun() {
      if (optimizer.Runs.Count > 1) { throw new ArgumentException("Should not contain more than one run."); }
      return optimizer.Runs.First();
    }

    private void Optimizer_Stopped(object sender, EventArgs e) {
      finishedEventHandle.Set();
    }


    private void Optimizer_ExecutionTimeChanged(object sender, EventArgs e) {
      if ((verbose && optimizer.ExecutionTime.Subtract(lastTimespan) > Helper.diffMinute)
        || (!verbose && optimizer.ExecutionTime.Subtract(lastTimespan) > Helper.diffHour)) {
        Helper.printToConsole(optimizer.ExecutionTime, taskname);

        lastTimespan = optimizer.ExecutionTime;
      }
    }

    private void Optimizer_Exception(object sender, EventArgs<Exception> e) {
      Helper.printToConsole("Optimizer Exception", taskname);
      Exception ex = e.Value;
      while (ex != null) {
        Helper.printToConsole(ex.Message, taskname);
        Helper.printToConsole(ex.StackTrace, taskname);
        Helper.printToConsole(Environment.NewLine, taskname);
        ex = ex.InnerException;
      }
    }
  }
}
