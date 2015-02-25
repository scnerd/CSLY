using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLY
{
    public abstract class Parser
    {
        internal static TerminalTokenType EOF = new TerminalTokenType("$");
        internal ParseRule[] Rules;
        internal ParseRule StartRule;

        internal Parser(ParseRule[] Rules, ParseRule StartRule)
        {
            this.Rules = Rules;
            this.StartRule = StartRule;
        }

        public ASTNode Parse(Lex Lexer, string Data)
        {
            return Parse(Lexer.Input(Data));
        }

        public abstract ASTNode Parse(IEnumerable<LexerToken> Data);

        internal ParseRule[] LookupRules(NonterminalTokenType Label)
        {
            return Rules.Where(r => r.OutputType == Label).ToArray();
        }
    }

    public abstract class ShiftReduceParser : Parser
    {

        protected Stack<ASTNode> Parsed = new Stack<ASTNode>();
        protected Stack<LexerToken> Unparsed = new Stack<LexerToken>();

        internal ShiftReduceParser(ParseRule[] Rules) : base(Rules)
        {
        }

        public override ASTNode Parse(IEnumerable<LexerToken> Data)
        {
            foreach (var tkn in Data.Reverse())
                Unparsed.Push(tkn);

            while (Step())
            {
            }

            if (Parsed.Count() != 1)
                throw new InvalidDataException(
                    "The given data could not be completely parsed, resulting in not exactly 1 AST node as the root");

            return Parsed.Pop();
        }

        protected abstract bool Step();
    }

    public class ParseTable
    {

    }

    public abstract class LRParser : ShiftReduceParser
    {

        protected class State : Dictionary<string, ParseAction>
        {
            internal int StateNum;
        }

        protected abstract class ParseAction
        {
        }

        protected class Reduce : ParseAction
        {
            internal readonly ParseRule Rule;
        }

        protected class Shift : ParseAction
        {
            internal readonly State GotoState;
        }

        protected class Done : ParseAction
        {
        }

        protected class Error : ParseAction
        {
            internal readonly string Message;
        }

        protected class Goto : ParseAction
        {
            internal readonly State GotoState;
        }

        protected ParseTable Table;
        protected int CurStateNum = -1;

        internal LRParser(ParseRule[] Rules) : base(Rules)
        {
            Table = GenerateParseTable(Rules);
        }

        internal abstract ParseTable GenerateParseTable(ParseRule[] Rules);

        protected State NewState()
        { return new State() {StateNum = ++CurStateNum}; }

        protected override bool Step()
        {
            throw new NotImplementedException();
        }
    }

    public class LR1Parser : LRParser
    {
        protected class ClosureComparer : IEqualityComparer<Closure>
        {
            public bool Equals(Closure x, Closure y)
            {
                return x.Rule == y.Rule && x.Lookahead == y.Lookahead && x.Position == y.Position;
            }

            public int GetHashCode(Closure obj)
            {
                return obj.GetHashCode();
            }
        }

        protected class Closure
        {
            internal ParseRule Rule;
            public int Position;
            internal ParseTokenType Lookahead;
        }

        internal LR1Parser(ParseRule[] Rules)
            : base(Rules)
        {
        }

        internal override ParseTable GenerateParseTable(ParseRule[] Rules)
        {
            StepThroughRule(new Closure()
            {
                Lookahead = EOF,
                Position = 0,
                Rule = StartRule
            });
        }

        private Closure[] Expand(Closure StartingPoint)
        {
            /* This ain't right or finished... Need to think this through better
            var Current = StartingPoint.Position < StartingPoint.Rule.Body.Length
                ? StartingPoint.Rule.Body[StartingPoint.Position]
                : null;
            if (Current == null)
            {
                return new Closure[0];
            }

            HashSet<Closure> FoundSet = new HashSet<Closure>(new LR1Parser.ClosureComparer());
            FoundSet.Add(StartingPoint);

            var Next = StartingPoint.Position < StartingPoint.Rule.Body.Length - 1
                ? StartingPoint.Rule.Body[StartingPoint.Position + 1]
                : StartingPoint.Lookahead;
            List<ParseTokenType> NewFounds = new List<ParseTokenType>();
            NewFounds.Add(Current);

            var PrevFoundCount = 0;
            while (PrevFoundCount != FoundSet.Count())
            {
                PrevFoundCount = FoundSet.Count();
                var NewFoundsTemp = NewFounds;
                NewFounds = new List<ParseTokenType>();
                foreach(var Found in NewFoundsTemp)
                    if (Found is NonterminalTokenType)
                    {
                foreach (var SubRule in LookupRules((NonterminalTokenType)Found))
                {
                    FoundSet.Add(new Closure() {Lookahead = Next, Position = 0, Rule = SubRule});
                }
                    }
                    else if (Found is TerminalTokenType)
                    {
                        FoundSet.Add(new Closure(){Lookahead = Next, Position = 0, });
                    }
                    else
                    {
                        throw new InvalidOperationException("Got a non-TokenType value in a parse rule");
                    }
            }
             */
        }

        internal void StepThroughRule(Closure Current)
        {
            foreach (var Considering in Expand(Current))
            {
            }
        }
    }

    public class SLRParser : LRParser
    {
        internal SLRParser(ParseRule[] Rules) : base(Rules)
        {
        }

        internal override ParseTable GenerateParseTable(ParseRule[] Rules)
        {
            throw new NotImplementedException();
        }
    }
}
