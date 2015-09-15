using CommandLine;
using CommandLine.Text;

namespace HeuristicLab.ConsoleApplication {
  public class Options {
    //[Option('e', "experiment", Required = true,
    //  HelpText = "File which contains the experiment which should be run (Algorithm, BatchRun, Experiment).")]
    [ValueOption(0)]
    public string InputFile { get; set; }

    [Option('r', "repetitions", Required = false, DefaultValue = 1,
      HelpText = "Number of repetitions")]
    public int Repetitions { get; set; }

    [Option('v', "verbose", DefaultValue = true,
      HelpText = "Prints all messages to standard output.")]
    public bool Verbose { get; set; }

    [ParserState]
    public IParserState LastParserState { get; set; }

    [HelpOption]
    public string GetUsage() {
      return HelpText.AutoBuild(this,
        (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
    }
  }
}
