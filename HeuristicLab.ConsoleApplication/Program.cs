using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeuristicLab.Common;
using HeuristicLab.Core;

namespace HeuristicLab.ConsoleApplication {
  public class Program {
    public static void Main(string[] args) {
      try {
        var options = new Options();
        if (CommandLine.Parser.Default.ParseArguments(args, options)) {
          Console.WriteLine(String.Format("Used command: {0}", String.Join(" ", args.Select(x => x.Contains(" ") ? String.Format("\"{0}\"", x) : x))));

          options.InputFiles = ExpandWildcards(options.InputFiles);

          // initialize ContentManager once
          ContentManager.Initialize(new PersistenceContentManager());

          switch (options.Start) {
            case RunAs.split:
              (new SplitToSingleRuns()).Start(options);
              break;
            case RunAs.breakup:
              (new BreakupExperiment()).Start(options);
              break;
            case RunAs.collect:
              (new CollectRuns()).Start(options);
              break;
            case RunAs.reevaluate:
              (new ReevaluateCFGPythonSolutionsInResultCollection()).Start(options);
              break;
            case RunAs.update:
              (new HL13To14UpdateFileFixer()).Start(options);
              break;
            case RunAs.check:
              (new CheckResults()).Start(options);
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

        Console.WriteLine("Exit with ERRORS!");
      }
    }

    // as shown here: http://stackoverflow.com/questions/381366/is-there-a-wildcard-expansion-option-for-net-apps#answer-2819150
    private static List<string> ExpandWildcards(IList<string> inputFiles) {
      var fileList = new List<string>();
      foreach (var filepattern in inputFiles) {
        var substitutedArg = Environment.ExpandEnvironmentVariables(filepattern);

        var dirPart = Path.GetDirectoryName(substitutedArg);
        dirPart = dirPart.Length == 0
                  ? dirPart = "."
                  : dirPart;

        var filePart = Path.GetFileName(substitutedArg);

        var files = Directory.GetFiles(dirPart, filePart);
        if (files.Length == 0) {
          fileList.Add(filepattern);
        } else {
          fileList.AddRange(Directory.GetFiles(dirPart, filePart));
        };
      }
      return fileList;
    }
  }
}
