using System.Collections.Generic;
using System.Linq;
using HeuristicLab.Encodings.SymbolicExpressionTreeEncoding;
using HeuristicLab.Problems.CFG;

namespace HeuristicLab.ConsoleApplication {
  public class GrammarPossibilities : IRunStrategy {

    private static string BNF = @"<e> ::= <e> <o> <e> | <v> | 1 | 2

<o> ::= + | - | *

<v> ::= a | b";

    //    private static string BNF = @"<expr> ::= <val> <op> <val> | <val> | 1 | 2
    //
    //<op> ::= + | - | *
    //
    //<val> ::= N_m | N_s";


    public void Start(Options options) {
      CFGParser pars = new CFGParser();

      var grammar = pars.readGrammarBNF(BNF);

      long possibilities = CalcPossibilitiesOfDepth(grammar, 2);
      System.Console.WriteLine(possibilities);
    }

    Dictionary<ISymbol, Dictionary<int, int>> depthPerSymbolTrees = new Dictionary<ISymbol, Dictionary<int, int>>();

    private long CalcPossibilitiesOfDepth(CFGExpressionGrammar grammar, int depth) {
      long pos = 0;

      var terminalSymbols = grammar.AllowedSymbols.Where(x => x.MaximumArity <= 0 && !(x is GroupSymbol));
      var nonterminalSymbols = grammar.AllowedSymbols.Where(x => x.MinimumArity > 0 && !(x is ProgramRootSymbol) && !(x is StartSymbol) && !(x is Defun) && (x is CFGProduction || !(x is CFGSymbol)));

      var startSymbols = grammar.GetAllowedChildSymbols(grammar.StartSymbol, 0);

      foreach (var symbol in nonterminalSymbols) {
        depthPerSymbolTrees.Add(symbol, new Dictionary<int, int>());
      }

      for (int i = 2; i < depth + 1; i++) {
        foreach (var ntSymbol in nonterminalSymbols) {
          int symPos = 1;

          for (int j = 0; j < ntSymbol.MaximumArity; j++) {
            int symbolArityPos = 0;

            foreach (var childSymbol in grammar.GetAllowedChildSymbols(ntSymbol, j)) {
              if (terminalSymbols.Contains(childSymbol)) {
                symbolArityPos++;
              } else {
                if (depthPerSymbolTrees[childSymbol].ContainsKey(i - 1)) {
                  symbolArityPos += depthPerSymbolTrees[childSymbol][i - 1];
                }
              }
            }
            symPos *= symbolArityPos;
          }

          depthPerSymbolTrees[ntSymbol].Add(i, symPos);
        }
      }

      foreach (var sy in startSymbols) {
        if (depthPerSymbolTrees.ContainsKey(sy)) {
          pos += depthPerSymbolTrees[sy].ContainsKey(depth) ? depthPerSymbolTrees[sy][depth] : 0;
        } else {
          pos += 1;
        }
      }

      return pos;
    }
  }
}
