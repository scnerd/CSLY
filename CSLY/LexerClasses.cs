using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLY
{
    public class MatchString : Attribute
    {
        internal string Matcher;
        public MatchString(string MatchRegex)
        {
            Matcher = MatchRegex;
        }
    }

    public class LexerToken
    {
        public Lex Lexer;
        public string Type;
        public object Value; // Will always initially be a string
        public int LineNo, LexPos;
    }
}
