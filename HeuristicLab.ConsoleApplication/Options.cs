using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace HeuristicLab.ConsoleApplication {
  public class Options {
    [Option('s', "start", HelpText = "The output files to generate.", DefaultValue = RunAs.aio)]
    public RunAs Start { get; set; }

    [ValueList(typeof(List<string>))]
    public List<string> InputFiles { get; set; }

    [Option('e', "startseed", HelpText = "Define the start seed for all input files. For every file the first optimizer will be set to start seed and then the seed will be incremented. -1 if no seed should be set (Default: -1)", Required = false, DefaultValue = -1)]
    public int StartSeed { get; set; }

    [Option('r', "repetitions", HelpText = "Define number of repitions. Default is 1. BatchRun etc. will also be run multiple times.", Required = false, DefaultValue = 1)]
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
