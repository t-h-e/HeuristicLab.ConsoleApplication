using System;
using System.Threading;
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

          int count = options.InputFiles.Count;
          HeuristicLabWorkingThread[] hlWorkingThread = new HeuristicLabWorkingThread[count];
          Thread[] hlThreads = new Thread[count];
          WaitHandle[] finishedWaitHandles = new WaitHandle[count];

          for (int i = 0; i < count; i++) {
            ManualResetEventSlim finishedEvent = new ManualResetEventSlim(false, 1);
            finishedWaitHandles[i] = finishedEvent.WaitHandle;


            hlWorkingThread[i] = new HeuristicLabWorkingThread(options.InputFiles[i], options.Repetitions, finishedEvent, options.Verbose);
            hlThreads[i] = new Thread(new ThreadStart(hlWorkingThread[i].Run));
            hlThreads[i].Start();
          }

          WaitHandle.WaitAll(finishedWaitHandles);

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
