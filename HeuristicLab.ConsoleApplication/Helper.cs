using System;
using System.Text;

namespace HeuristicLab.ConsoleApplication {
  public class Helper {

    public static readonly TimeSpan diffHour = new TimeSpan(1, 0, 0); // one hour
    public static readonly TimeSpan diffMinute = new TimeSpan(0, 1, 0); // one minute

    public static void printToConsole(object value, string fileName) {
      printToConsole(value.ToString(), fileName);
    }

    public static void printToConsole(string value, string fileName) {
      StringBuilder strBuilder = new StringBuilder(DateTime.Now.ToString());
      strBuilder.Append(" ");
      strBuilder.Append(fileName);
      strBuilder.Append(": ");
      strBuilder.Append(value);
      Console.WriteLine(strBuilder.ToString());
    }
  }
}
