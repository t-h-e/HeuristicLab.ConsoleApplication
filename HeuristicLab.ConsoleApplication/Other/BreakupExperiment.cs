using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeuristicLab.Common;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class BreakupExperiment : IRunStrategy {

    public void Start(Options options) {
      foreach (string filePath in options.InputFiles) {
        string fileName = Path.GetFileName(filePath);
        var experiment = LoadExperiment(filePath);
        if (experiment == null) {
          Helper.printToConsole("Error: This is not an experiment.", Path.GetFileName(filePath));
          continue;
        }

        experiment.Prepare();
        experiment.Runs.Clear();

        Dictionary<string, IOptimizer> store;
        if (experiment.Optimizers.Select(o => o.Name).Distinct().Count() == experiment.Optimizers.Count) {
          store = experiment.Optimizers.ToDictionary(o => o.Name);
        } else {
          Helper.printToConsole("Warning: Optimizer names are not unique.", Path.GetFileName(filePath));
          store = new Dictionary<string, IOptimizer>();
          foreach (var opt in experiment.Optimizers) {
            if (!store.ContainsKey(opt.Name)) {
              store.Add(opt.Name, opt);
            } else {
              int count = 1;
              while (store.ContainsKey(String.Format("{0} {1}", opt.Name, count))) {
                count++;
              }
              store.Add(opt.Name, opt);
            }
          }
        }

        foreach (var opt in store) {
          var storable = opt.Value as IStorableContent;
          if (storable == null) {
            Helper.printToConsole(String.Format("{0} is not a storeable content", opt.Key), Path.GetFileName(filePath));
            continue;
          }
          ContentManager.Save(storable, String.Format("{0}.hl", opt.Key), true);
        }
      }
    }


    private Experiment LoadExperiment(string filePath) {
      Helper.printToConsole("Loading...", Path.GetFileName(filePath));
      var content = ContentManager.Load(filePath);

      Helper.printToConsole("Loading completed!", Path.GetFileName(filePath));
      Helper.printToConsole("Content loaded: " + content.ToString(), Path.GetFileName(filePath));

      return content as Experiment;
    }
  }
}
