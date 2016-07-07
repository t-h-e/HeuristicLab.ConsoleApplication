using System.IO;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class HLRunInfo {
    public int Id { get; private set; }
    public IOptimizer Optimizer { get; private set; }
    public string FilePath { get; private set; }

    public string FileName { get { return Path.GetFileName(FilePath); } }
    public int CoresRequired { get; private set; }
    public string SavePath { get; private set; }

    public HLRunInfo(int id, IOptimizer optimizer, string filePath, int coresRequired, string savePath) {
      Id = id;
      Optimizer = optimizer;
      FilePath = filePath;
      CoresRequired = coresRequired;
      SavePath = savePath;
    }

    public void RemoveOptimizer() {
      Optimizer = null;
    }
  }
}
