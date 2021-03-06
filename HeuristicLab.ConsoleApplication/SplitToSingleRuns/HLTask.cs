﻿using System;
using System.Linq;
using System.Threading;
using HeuristicLab.Common;
using HeuristicLab.Optimization;
using HeuristicLab.Problems.CFG.Python;

namespace HeuristicLab.ConsoleApplication {
  public class HLTask {

    private ManualResetEventSlim finishedEventHandle;

    public HLRunInfo runInfo;

    public IOptimizer Optimizer { get { return runInfo.Optimizer; } }

    private TimeSpan lastTimespan;
    private string lastGenerationUpdate;

    private bool verbose;

    private bool finishedSuccessfully = true;

    public HLTask(HLRunInfo runInfo, bool verbose) {
      this.runInfo = runInfo;
      this.verbose = verbose;
    }

    public bool Start() {
      RegisterEvents();

      lastGenerationUpdate = String.Empty;
      lastTimespan = TimeSpan.Zero;
      finishedEventHandle = new ManualResetEventSlim(false, 1);
      Optimizer.Start();
      finishedEventHandle.Wait();

      if (Optimizer.ExecutionState == Core.ExecutionState.Started || Optimizer.ExecutionState == Core.ExecutionState.Paused) {
        Optimizer.Stop();
      }

      ContentManager.Save(new RunCollection(GetRun().ToEnumerable()), runInfo.SavePath, false);

      DeregisterEvents();
      finishedEventHandle.Dispose();
      return finishedSuccessfully;
    }

    private void RegisterEvents() {
      Optimizer.Stopped += new EventHandler(Optimizer_Stopped);
      Optimizer.ExecutionTimeChanged += new EventHandler(Optimizer_ExecutionTimeChanged);
      Optimizer.ExceptionOccurred += new EventHandler<EventArgs<Exception>>(Optimizer_Exception);
    }

    private void DeregisterEvents() {
      Optimizer.Stopped -= new EventHandler(Optimizer_Stopped);
      Optimizer.ExecutionTimeChanged -= new EventHandler(Optimizer_ExecutionTimeChanged);
      Optimizer.ExceptionOccurred -= new EventHandler<EventArgs<Exception>>(Optimizer_Exception);
    }



    private IRun GetRun() {
      if (Optimizer.Runs.Count > 1) { throw new ArgumentException("Should not contain more than one run."); }
      return Optimizer.Runs.Count < 1 ? null : Optimizer.Runs.First();
    }

    private void Optimizer_Stopped(object sender, EventArgs e) {
      finishedEventHandle.Set();
    }


    private void Optimizer_ExecutionTimeChanged(object sender, EventArgs e) {
      if ((verbose && Optimizer.ExecutionTime.Subtract(lastTimespan) > Helper.diffMinute)
        || (!verbose && Optimizer.ExecutionTime.Subtract(lastTimespan) > Helper.diffHour)) {
        string newGenerationUpdate = GetGeneration(Optimizer);
        Helper.printToConsole(Optimizer.ExecutionTime + "; " + newGenerationUpdate, String.Format("{0}({1})", runInfo.FileName, runInfo.Id));

        if (newGenerationUpdate == lastGenerationUpdate) {
          printPythonIndividuals(Optimizer);
        }

        lastGenerationUpdate = newGenerationUpdate;
        lastTimespan = Optimizer.ExecutionTime;
      }
    }

    private void printPythonIndividuals(IOptimizer optimizer) {
      var algo = optimizer as Algorithm;
      if (algo == null) return;
      var prob = algo.Problem as CFGPythonProblem;
      if (prob == null) return;

      Helper.printToConsole(String.Join(Environment.NewLine, prob.PythonProcess.GetIndidividuals()), String.Format("{0}({1})", runInfo.FileName, runInfo.Id));
    }

    private void Optimizer_Exception(object sender, EventArgs<Exception> e) {
      Helper.printToConsole(e.Value, String.Format("{0}({1})", runInfo.FileName, runInfo.Id), "Optimizer Exception");
      finishedSuccessfully = false;
      finishedEventHandle.Set();
    }

    private string GetGeneration(IOptimizer opt) {
      var engineAlgorithm = opt as EngineAlgorithm;
      if (engineAlgorithm == null) {
        engineAlgorithm = opt.NestedOptimizers.Where(o => o is EngineAlgorithm
         && o.ExecutionState.Equals(HeuristicLab.Core.ExecutionState.Started)).FirstOrDefault() as EngineAlgorithm;
      }

      if (engineAlgorithm != null && engineAlgorithm.Results.ContainsKey("Generations")) {
        return engineAlgorithm.Results["Generations"].ToString();
      }

      return "No generation info found.";
    }
  }
}
