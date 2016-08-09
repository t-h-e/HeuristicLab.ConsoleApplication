using System;
using System.IO;
using System.Linq;
using HeuristicLab.Common;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;
using HeuristicLab.Optimization;
using HeuristicLab.Problems.CFG.Python;

namespace HeuristicLab.ConsoleApplication {
  public class ReevaluateCFGPythonSolutionsInResultCollection : IRunStrategy {
    private static string BESTTRAININGSOLUTION = "Best training solution";

    public void Start(Options options) {
      foreach (var filePath in options.InputFiles) {
        Reevaluate(filePath, options.Timeout);
      }
    }

    private void Reevaluate(string filePath, double timeout) {
      string fileName = Path.GetFileName(filePath);
      var runcollection = LoadRunCollection(filePath);
      if (runcollection == null) {
        Helper.printToConsole("Error: This is not an experiment.", fileName);
        return;
      }

      foreach (var run in runcollection) {
        var model = run.Results[BESTTRAININGSOLUTION + ".Model"] as ISymbolicExpressionTree;
        foreach (var name in run.Results.Select(x => x.Key).Where(x => x.StartsWith(BESTTRAININGSOLUTION + ".") || x.Equals(BESTTRAININGSOLUTION)).ToList()) {
          run.Results.Remove(name);
        }

        var problemData = run.Parameters["CFGProblemData"] as CFGPythonProblemData;
        var pythonProcess = run.Parameters["PythonProcess"] as PythonProcess;
        pythonProcess.DegreeOfParallelism = 1;
        if (problemData != null && pythonProcess != null) {
          var solution = new CFGPythonSolution(model, problemData, timeout, pythonProcess);
          run.Results.Add(BESTTRAININGSOLUTION, solution as ResultCollection);
          foreach (var item in solution) {
            run.Results.Add(String.Format("{0}.{1}", BESTTRAININGSOLUTION, item.Name), item.Value);
          }
          pythonProcess.Dispose();
        } else {
          Helper.printToConsole("Warning: Could not reevaluate solution.", fileName);
        }
      }
      Helper.printToConsole("Saving...", fileName);
      ContentManager.Save(runcollection, filePath, true);
      Helper.printToConsole("Saved", fileName);
    }

    private RunCollection LoadRunCollection(string filePath) {
      Helper.printToConsole("Loading...", Path.GetFileName(filePath));
      var content = ContentManager.Load(filePath);

      Helper.printToConsole("Loading completed!", Path.GetFileName(filePath));
      Helper.printToConsole("Content loaded: " + content.ToString(), Path.GetFileName(filePath));

      return content as RunCollection;
    }
  }
}
