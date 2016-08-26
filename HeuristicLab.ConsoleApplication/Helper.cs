using System;
using System.Globalization;
using System.IO;
using System.Text;
using HeuristicLab.Common;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class Helper {

    private static Object writeLock = new Object();

    private static CultureInfo culture = CultureInfo.CreateSpecificCulture("en-gb");

    public static readonly TimeSpan diffHour = new TimeSpan(1, 0, 0); // one hour
    public static readonly TimeSpan diffMinute = new TimeSpan(0, 1, 0); // one minute

    public static void printToConsole(Exception ex, string fileName, string message = "") {
      if (String.IsNullOrEmpty(message)) Helper.printToConsole(message, fileName);
      while (ex != null) {
        Helper.printToConsole(ex.Message, fileName);
        Helper.printToConsole(ex.StackTrace, fileName);
        Helper.printToConsole(Environment.NewLine, fileName);
        ex = ex.InnerException;
      }
    }

    public static void printToConsole(object value, string fileName) {
      printToConsole(value.ToString(), fileName);
    }

    public static void printToConsole(string value, string fileName) {
      StringBuilder strBuilder = new StringBuilder(DateTime.Now.ToString("G", culture));
      strBuilder.Append(" ");
      strBuilder.Append(fileName);
      strBuilder.Append(": ");
      strBuilder.Append(value);
      lock (writeLock) {
        Console.WriteLine(strBuilder.ToString());
      }
    }


    public static RunCollection LoadRunCollection(string filePath) {
      Helper.printToConsole("Loading...", Path.GetFileName(filePath));
      var content = ContentManager.Load(filePath);

      Helper.printToConsole("Loading completed!", Path.GetFileName(filePath));
      Helper.printToConsole("Content loaded: " + content.ToString(), Path.GetFileName(filePath));

      return content as RunCollection;
    }
  }
}
