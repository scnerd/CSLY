using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLY
{
    /// <summary>
    /// Represents a single rule in the parser (e.g., A -> A + B)
    /// </summary>
    public class ParseRule
    {
        internal readonly NonterminalTokenType OutputType;

        internal readonly ParseTokenType[] Body;
    }

    /// <summary>
    /// Represents a generic token that might appear in a rule (e.g., consider A -> A + B; A, +, and B would be ParseTokenTypes)
    /// </summary>
    public class ParseTokenType
    {
        internal readonly string Label;

        internal ParseTokenType(string Label)
        {
            this.Label = Label;
        }
    }

    /// <summary>
    /// Represents a terminal token that might appear in a rule (e.g., consider A -> A + B; + would be a terminal token)
    /// </summary>
    public class TerminalTokenType : ParseTokenType
    {
        internal TerminalTokenType(string Label)
            : base(Label)
        {
        }

        public static implicit operator TerminalTokenType(ParseRule rule)
        {
            return new TerminalTokenType(rule.OutputType);
        }
    }

    /// <summary>
    /// Represents a terminal token that might appear in a rule (e.g., consider A -> A + B; A and B would be a non-terminal token)
    /// </summary>
    public class NonterminalTokenType : ParseTokenType
    {
        internal NonterminalTokenType(string Label) : base(Label)
        {
        }

        public static implicit operator NonterminalTokenType(string token)
        {
            return new NonterminalTokenType(token);
        }
    }

    /// <summary>
    /// Represents an output AST node, generated from actual parsed text
    /// </summary>
    public class ASTNode
    {
        public NonterminalTokenType Type;
        public object Value;
        public ASTNode[] Children;

        public ASTNode this[int index]
        { get { return Children[index]; } }
    }
}
