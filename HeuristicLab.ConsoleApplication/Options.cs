using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace HeuristicLab.ConsoleApplication {
  public class Options {
    [Option(Required = true, HelpText = "Files which contain experiments which should be run (Algorithm, BatchRun, Experiment).")]
    [ValueList(typeof(List<string>))]
    public List<string> InputFiles { get; set; }

    [Option('r', "repetitions", Required = false, DefaultValue = 1,
      HelpText = "Number of repetitions")]
    public int Repetitions { get; set; }

    [Option('v', "verbose", DefaultValue = false,
      HelpText = "Output time information from runs in intervals of minutes, default is hourly")]
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
