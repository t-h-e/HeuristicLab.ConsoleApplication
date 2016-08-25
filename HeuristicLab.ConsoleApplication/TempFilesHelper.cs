using System;
using System.IO;

namespace HeuristicLab.ConsoleApplication {
  public class TempFilesHelper {

    private static string TMPDIR;
    private static TempFilesHelper instance;

    private TempFilesHelper() {
      string newTmpDir = String.Empty;
      do {
        newTmpDir = Path.GetRandomFileName();
      } while (Directory.Exists(newTmpDir));

      Directory.CreateDirectory(newTmpDir);
      TMPDIR = Path.Combine(Directory.GetCurrentDirectory(), newTmpDir);

      Console.WriteLine(String.Format("Created Temporary Experiment Folder: {0}", TMPDIR));
    }

    public static TempFilesHelper GetInstance() {
      if (instance == null) {
        instance = new TempFilesHelper();
      }
      return instance;
    }

    public string GetTempFileName() {
      return Path.Combine(TMPDIR, Path.GetRandomFileName());
    }
  }
}
