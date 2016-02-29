using System;
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

          if (false) {
            (new AllInOne()).Start(options);
          } else {
            (new SplitToSingleRuns()).Start(options);
          }

          //(new GrammarPossibilities()).Start(options);

          Console.WriteLine("All Threads finished successfully");
        }
      }
      catch (Exception e) {
        Console.WriteLine(e.Message);
        Console.WriteLine(e.StackTrace);
      }
    }
  }
}
