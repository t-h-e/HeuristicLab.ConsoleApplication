using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeuristicLab.Common;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class CollectRuns : IRunStrategy {
    private const string LOADINGCOMPLETE = "Loading completed!";
    private const string CONTENTLOADED = "Content loaded: ";
    private const string SAVEPATH = "Temporary save path: ";

    public void Start(Options options) {
      foreach (var filePath in options.InputFiles) {
        Collect(filePath);
      }
    }

    private void Collect(string filePath) {
      string fileName = Path.GetFileNameWithoutExtension(filePath);
      string[] lines = File.ReadAllLines(filePath);
      int pos = lines.Length - 1;
      while (!lines[pos].Contains(LOADINGCOMPLETE) && pos >= 0) {
        pos--;
      }

      if (pos < 0) {
        Console.WriteLine("Could not find a successfully loaded file.");
        return;
      }

      pos++;
      var namePos = lines[pos].IndexOf(CONTENTLOADED) + CONTENTLOADED.Length;
      var name = lines[pos].Substring(namePos, lines[pos].Length - namePos);

      pos++;
      int pathPos = -1;
      List<string> tmpPaths = new List<string>();
      while (pos < lines.Length && (pathPos = lines[pos].IndexOf(SAVEPATH)) >= 0) {
        pathPos += SAVEPATH.Length;
        tmpPaths.Add(lines[pos].Substring(pathPos, lines[pos].Length - pathPos));
        pos++;
      }

      Helper.printToConsole("Saving...", fileName);
      RunCollection collectedRuns = new RunCollection();
      foreach (var path in tmpPaths.Where(x => File.Exists(x) && new FileInfo(x).Length > 0)) {
        try {
          var runCollection = ContentManager.Load(path) as RunCollection;
          if (runCollection != null) {
            collectedRuns.AddRange(runCollection);
          } else {
            Helper.printToConsole(String.Format("WARNING: {0} is not a run collection.", path), fileName);
          }
        } catch (Exception e) {
          Helper.printToConsole("Exception while collecting: " + e.Message, fileName);
        }
      }

      ContentManager.Save(collectedRuns, name + "-Results.hl", true);
    }
  }
}
