using System.Threading;

namespace HeuristicLab.ConsoleApplication {
  public class AllInOne : IRunStrategy {
    public void Start(Options options) {
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
    }
  }
}
