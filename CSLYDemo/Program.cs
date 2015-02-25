using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CSLY;
using Helpers;

namespace CSLYDemo
{
    public class Program
    {
        public static string[] Tokens = new[] { "NAME", "NUMBER", "PLUS", "MINUS", "TIMES", "DIVIDE", "EQUALS", "LPAREN", "RPAREN"};

        public static string
            t_PLUS = @"\+",
            t_MINUS = @"-",
            t_TIMES = @"\*",
            t_DIVIDE = @"/",
            t_EQUALS = @"=",
            t_LPAREN = @"\(",
            t_RPAREN = @"\)",
            t_NAME = @"[a-zA-Z_][a-zA-Z0-9_]*";

        public static string t_ignore = " \t";

        [MatchString(@"\d+")]
        public static LexerToken t_NUMBER(LexerToken Tok)
        {
            int val;
            if (int.TryParse((string) Tok.Value, out val))
                Tok.Value = val;
            else
            {
                Console.WriteLine("Integer value too large " + Tok.Value.ToString());
                Tok.Value = 0;
            }
            return Tok;
        }

        [MatchString(@"\r?\n")]
        public static LexerToken t_newline(LexerToken Tok)
        {
            Tok.Lexer["LineNo"] = ((string) Tok.Value).Where(c => c == '\n').Count() + (int)Tok.Lexer["LineNo"];
            return null;
        }

        [MatchString(null)]
        public static LexerToken t_error(LexerToken Tok)
        {
            Console.WriteLine("Illegal character: " + ((string)Tok.Value)[0]);
            return null;
        }

        static void Main(string[] args)
        {
            var lexer = new Lex(typeof (Program));
            //Console.WriteLine(lexer.Report());
            foreach (var tkn in lexer.Input("3 + 5 * ( 10-20 )"))
            {
                Console.WriteLine("Token: {0} '{1}'".QuickFormat(tkn.Type, tkn.Value.ToString()));
            }
            Console.ReadLine();
        }
    }
}
