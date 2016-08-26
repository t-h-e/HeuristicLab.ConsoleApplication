using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeuristicLab.Data;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class CheckResults : IRunStrategy {
    public void Start(Options options) {
      var reeval = new ReevaluateCFGPythonSolutionsInResultCollection();

      foreach (var filePath in options.InputFiles) {
        RunCollection coll = Helper.LoadRunCollection(filePath);
        if (Check(coll, filePath) == 2) {
          reeval.Reevaluate(coll, filePath, options.Timeout);
          Helper.printToConsole("Check again", Path.GetFileName(filePath));
          coll = Helper.LoadRunCollection(filePath);
          Check(coll, filePath);
        }
      }
    }

    /// <summary>
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>0 == everything ok; 1 == seed problem; 2 == probably timeout</returns>
    private int Check(RunCollection coll, string filePath) {
      string fileName = Path.GetFileName(filePath);


      List<int> seeds = Enumerable.Range(1, 100).ToList();

      int count = 0;
      foreach (var run in coll) {
        if (!seeds.Remove(((IntValue)run.Parameters["Seed"]).Value)) {
          Helper.printToConsole("Seed problem", fileName);
          return 1;
        }
        double test = ((DoubleValue)run.Results["Best training solution.Test Quality"]).Value;
        if (double.IsNaN(test)) {
          Helper.printToConsole("Probably timeout", fileName);
          return 2;
        } else if (test < 0.000000001) {
          count++;
        }
      }

      if (seeds.Count > 0) {
        Helper.printToConsole("Too few runs", fileName);
        return 1;
      }

      Helper.printToConsole(string.Format("Successful: {0} of {1}", count, coll.Count), fileName);

      return 0;
    }
  }
}
