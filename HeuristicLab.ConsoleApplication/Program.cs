using System;
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

          CreateTempExperimentFolder();

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

    private static void CreateTempExperimentFolder() {
      string newTmpDir = String.Empty;
      do {
        newTmpDir = Path.GetRandomFileName();
      } while (Directory.Exists(newTmpDir));

      Directory.CreateDirectory(newTmpDir);
      string tmpDir = Path.Combine(Directory.GetCurrentDirectory(), newTmpDir);

      Console.WriteLine(String.Format("Created Temporary Experiment Folder: {0}", tmpDir));

      Environment.SetEnvironmentVariable("TMP", tmpDir);    // Windows
      Environment.SetEnvironmentVariable("TMPDIR", tmpDir); // Everything else
    }
  }
}
