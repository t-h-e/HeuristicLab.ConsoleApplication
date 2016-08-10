using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace HeuristicLab.ConsoleApplication {
  public class HL13To14UpdateFileFixer : IRunStrategy {
    public void Start(Options options) {
      foreach (var file in options.InputFiles) {
        string fileName = Path.GetFileName(file);
        using (var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite)) {
          using (var zipFile = new ZipArchive(stream, ZipArchiveMode.Update)) {
            ZipArchiveEntry data = zipFile.Entries.Where(x => x.FullName.Equals("data.xml")).FirstOrDefault();
            ZipArchiveEntry typecache = zipFile.Entries.Where(x => x.FullName.Equals("typecache.xml")).FirstOrDefault();

            string tmp = null;
            XmlDocument doc = new XmlDocument();
            using (var s = new StreamReader(data.Open())) {
              tmp = s.ReadToEnd();
            }
            doc.LoadXml(tmp);
            var primitiveNode = doc.SelectNodes("//PRIMITIVE[contains(.,'GEArtificialAntEvaluator')]");
            if (primitiveNode.Count > 1 || primitiveNode.Count <= 0) {
              Helper.printToConsole("No GEArtificialAntEvaluator found", fileName);
              continue;
            }
            primitiveNode[0].ParentNode.ParentNode.RemoveChild(primitiveNode[0].ParentNode);

            string name = data.FullName;
            data.Delete();
            data = zipFile.CreateEntry(name);
            using (var s = new StreamWriter(data.Open())) {
              doc.Save(s);
            }

            using (var s = new StreamReader(typecache.Open())) {
              tmp = s.ReadToEnd();
            }
            tmp = string.Join(Environment.NewLine, tmp.Split(new string[] { Environment.NewLine }, StringSplitOptions.None).Where(x => !x.Contains("GrammaticalEvolution")).ToArray());
            name = typecache.FullName;
            typecache.Delete();
            typecache = zipFile.CreateEntry(name);
            using (var s = new StreamWriter(typecache.Open())) {
              s.Write(tmp);
            }
          }
        }
      }
    }
  }
}
