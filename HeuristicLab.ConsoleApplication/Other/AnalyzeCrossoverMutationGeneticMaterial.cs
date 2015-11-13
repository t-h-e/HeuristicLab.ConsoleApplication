using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HeuristicLab.Analysis;
using HeuristicLab.Common;
using HeuristicLab.Optimization;

namespace HeuristicLab.ConsoleApplication {
  public class AnalyzeCrossoverMutationGeneticMaterial : IRunStrategy {

    public void Start(Options options) {
      foreach (var filePath in options.InputFiles) {
        var content = ContentManager.Load(filePath);

        var runs = content as RunCollection;
        if (runs != null) {
          if (runs.Any(r => r.Results.ContainsKey("Crossover Actual CutPoint Parent"))) {
            CrossoverCalc(runs, Path.GetFileNameWithoutExtension(filePath));
          }

          if (runs.Any(r => r.Results.ContainsKey("Manipulator Actual CutPoint Parent"))) {
            ManipulatorCalc(runs, Path.GetFileNameWithoutExtension(filePath));
          }
        } else {
          throw new ArgumentException("no Run collection");
        }
      }
    }

    private static void CrossoverCalc(RunCollection runs, string filename) {
      DataTable parent = CalcAverage(runs, "Crossover Actual CutPoint Parent");

      DataTable removedLength = CalcAverage(runs, "CrossoverActualRemovedMaterialLength");
      DataTable removedDepth = CalcAverage(runs, "CrossoverActualRemovedMaterialDepth");
      DataTable addedLength = CalcAverage(runs, "CrossoverActualAddedMaterialLength");
      DataTable addedDepth = CalcAverage(runs, "CrossoverActualAddedMaterialDepth");

      var cummulativeAverageRemovedLength = CalcCummulativeAverage(parent, removedLength);
      var cummulativeAverageRemovedDepth = CalcCummulativeAverage(parent, removedDepth);
      var cummulativeAverageAddedLength = CalcCummulativeAverage(parent, addedLength);
      var cummulativeAverageAddedDepth = CalcCummulativeAverage(parent, addedDepth);

      WriteToCSV(new List<IEnumerable<double>>() { cummulativeAverageRemovedLength, cummulativeAverageRemovedDepth, cummulativeAverageAddedLength, cummulativeAverageAddedDepth },
        new List<string>() { "RemovedLenghtXO", "RemovedDepthXO", "AddedLenghtXO", "AddedDepthXO" }, filename + "_(XO).csv");

    }

    private static void ManipulatorCalc(RunCollection runs, string filename) {
      DataTable parent = CalcAverage(runs, "Manipulator Actual CutPoint Parent");

      DataTable removedLength = CalcAverage(runs, "ManipulatorActualRemovedMaterialLength");
      DataTable removedDepth = CalcAverage(runs, "ManipulatorActualRemovedMaterialDepth");
      DataTable addedLength = CalcAverage(runs, "ManipulatorActualAddedMaterialLength");
      DataTable addedDepth = CalcAverage(runs, "ManipulatorActualAddedMaterialDepth");

      var cummulativeAverageRemovedLength = CalcCummulativeAverage(parent, removedLength);
      var cummulativeAverageRemovedDepth = CalcCummulativeAverage(parent, removedDepth);
      var cummulativeAverageAddedLength = CalcCummulativeAverage(parent, addedLength);
      var cummulativeAverageAddedDepth = CalcCummulativeAverage(parent, addedDepth);

      WriteToCSV(new List<IEnumerable<double>>() { cummulativeAverageRemovedLength, cummulativeAverageRemovedDepth, cummulativeAverageAddedLength, cummulativeAverageAddedDepth },
        new List<string>() { "RemovedLenghtXO", "RemovedDepthXO", "AddedLenghtXO", "AddedDepthXO" }, filename + "_(MO).csv");

    }

    private static void WriteToCSV(IEnumerable<IEnumerable<double>> values, IEnumerable<string> header, string filename) {
      using (StreamWriter file = new StreamWriter(filename)) {
        file.WriteLine(String.Join(",", header));

        for (int i = 0; i < values.First().Count(); i++) {
          file.WriteLine(String.Join(",", values.Select(x => x.ElementAt(i))));

        }
      }
    }

    private static IEnumerable<double> CalcCummulativeAverage(DataTable parent, DataTable other) {
      IEnumerable<double> avg = Enumerable.Repeat(0.0, parent.Rows.First().Values.Count);
      IEnumerable<double> real = Enumerable.Repeat(0.0, parent.Rows.First().Values.Count);

      foreach (var rowName in parent.Rows.Select(x => x.Name)) {
        var symboleParentRow = parent.Rows[rowName];
        var symboleOtherRow = other.Rows[rowName];

        avg = avg.Zip(symboleParentRow.Values, (x, y) => x + y);
        real = real.Zip(symboleParentRow.Values.Zip(symboleOtherRow.Values, (x, y) => x * y), (x, y) => x + y);
      }

      return real.Zip(avg, (x, y) => x / y);
    }

    private static DataTable CalcAverage(RunCollection runs, string tableName) {
      DataTable temptable = new DataTable();

      var visibleRuns = runs.Where(r => r.Visible);

      var resultName = (string)tableName;

      var dataTables = visibleRuns.Where(r => r.Results.ContainsKey(resultName)).Select(r => (DataTable)r.Results[resultName]);
      if (dataTables.Count() != visibleRuns.Count()) {
        throw new ArgumentException("Should not happen");
      }

      var dataRows = dataTables.SelectMany(dt => dt.Rows).GroupBy(r => r.Name, r => r);

      foreach (var row in dataRows) {
        var averageValues = DataRowsAggregate(Enumerable.Average, row.Select(r => r.Values));
        DataRow averageRow = new DataRow(row.Key, "Average of Values", averageValues);
        temptable.Rows.Add(averageRow);
      }
      return temptable;
    }

    private static IEnumerable<double> DataRowsAggregate(Func<IEnumerable<double>, double> aggregate, IEnumerable<IEnumerable<double>> arrays) {
      return Enumerable.Range(0, arrays.First().Count())
        .Select(i => aggregate(arrays.Select(a => a.Skip(i).First()).Select(x => Double.IsNaN(x) ? 0 : x)));
    }
  }
}
