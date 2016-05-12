﻿using System;
using HeuristicLab.Common;
using HeuristicLab.Core;

namespace HeuristicLab.ConsoleApplication {
  public class Program {
    public static void Main(string[] args) {
      try {
        var options = new Options();
        if (CommandLine.Parser.Default.ParseArguments(args, options)) {

          // initialize ContentManager once
          ContentManager.Initialize(new PersistenceContentManager());

          switch (options.Start) {
            case RunAs.split:
              (new SplitToSingleRuns()).Start(options);
              break;
            case RunAs.breakup:
              (new BreakupExperiment()).Start(options);
              break;
            default:
              (new AllInOne()).Start(options);
              break;
          }

          //(new GrammarPossibilities()).Start(options);

          Console.WriteLine("All Threads finished successfully");
        }
      } catch (Exception e) {
        Console.WriteLine(e.Message);
        Console.WriteLine(e.StackTrace);
        while (e != null) {
          Console.WriteLine(e.Message);
          Console.WriteLine(e.StackTrace);
          e = e.InnerException;
        }
      }
    }
  }
}
