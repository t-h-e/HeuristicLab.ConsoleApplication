﻿using System.Collections.Generic;
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

    [Option('p', "parallelism", Required = false, DefaultValue = 0,
      HelpText = "Defines how many cores should be used. If set to 0, the values set in the HL file will be used. Can currently only be used with split! To use all available cores set to -1.")]
    public int Parallelism { get; set; }

    [Option('c', "continue", DefaultValue = false,
      HelpText = "If set only a single file can be set in input files, which indicates what experiment can should be continues ")]
    public bool Continue { get; set; }

    [Option('t', "timeput", HelpText = "Timeout used in Reevaluation only. Default: 1.0", Required = false, DefaultValue = 1.0)]
    public double Timeout { get; set; }

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
