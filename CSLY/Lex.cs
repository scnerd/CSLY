using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Helpers;

namespace CSLY
{
    public class Lex
    {
        /*
         * TODO: Allow beginning-of-line in input regex's (currently restricted for use w/in lexer)
         * TODO: Implement lexing states
         * TODO: Implement lexer cloning
         * TODO: Implement custom regex flag passing
         * TODO: Implement error handler helpers to allow correction and resuming
         */

        private class Token
        {
            public string Name, Matcher;
            public Func<LexerToken, LexerToken> OnMatch = new Func<LexerToken, LexerToken>(tk => tk);
            public bool Ignore = false;
            /* Default method:
             * [MatchString(STRING)]
             * public LexToken Body(LexToken Tok) { return Tok; }
             */

            public override string ToString()
            {
                return string.Format("Token({0}{2}: {1})", Name, Matcher, Ignore ? "!" : "");
            }
        }

        private const string TOKEN_PREFIX = "t_";
        private const string IGNORE_PREFIX = "ignore_";
        private const string SPECIAL_CHAR_IGNORE = "t_ignore";
        private const string SPECIAL_CHAR_LITERALS = "literals";
        private const string SPECIAL_ERROR = "t_error";

        private List<string> ReportItems = new List<string>();

        private Type LexerClass;
        private FieldInfo[] Fields;
        private MethodInfo[] Methods;
        private string[] TokenList;
        private Token[] Tokens;
        private string IgnoreChars = "";
        private Action<LexerToken> ErrorHandler = null;

        private List<Tuple<Regex, Token>> Mapper;

        private Dictionary<string, object> PublicProperties = new Dictionary<string, object>();

        public Lex(Type LexerClass)
        {
            this.LexerClass = LexerClass;
            GetContent();
            GetTokenList();
            CheckForCharIgnore();
            ParseTokens();
            CheckForLiterals();
            CheckForErrorHandler();
            FinalizeMapper();
        }

        private void GetContent()
        {
            Fields = LexerClass.GetFields().ToArray();
            Methods = LexerClass.GetMethods().ToArray();

            ReportItems.Add("Fields: " + ", ".Combine(Fields.Select(f => f.Name).ToArray()));
            ReportItems.Add("Methods: " + ", ".Combine(Fields.Select(m => m.Name).ToArray()));
        }

        private void GetTokenList()
        {
            var tokenList =
                Fields.Where(f => f.Name == "Tokens")
                    .Where(f => typeof (IEnumerable<string>).IsAssignableFrom(f.FieldType))
                    .Take(1)
                    .ElementAtOrDefault(0);
            if (tokenList == null)
                throw new MissingFieldException(
                    "No token list found (must inherit from IEnumberable<string> and be called 'tokens')");
            this.TokenList = ((IEnumerable<string>) tokenList.GetValue(null)).ToArray();

            ReportItems.Add("Token Names: " + ", ".Combine(TokenList.Select(f => f).ToArray()));
        }

        private void CheckMethod(MethodInfo ToCheck)
        {
            if (!ToCheck.GetCustomAttributes().Any(a => a is MatchString))
                throw new CustomAttributeFormatException(
                    "Method-defined tokens must be tagged with a MatchString attribute");
            if (!ToCheck.ReturnType.IsAssignableFrom(typeof (LexerToken)))
                throw new InvalidCastException("Method-defined tokens must return LexerToken");
            if (ToCheck.GetParameters().Length != 1 ||
                !ToCheck.GetParameters()[0].ParameterType.IsAssignableFrom(typeof (LexerToken)))
                throw new TargetParameterCountException(
                    "Method-defined tokens must take exactly one attribute of type LexerToken");
            if (ToCheck.GetMethodBody() == null)
                throw new MissingMethodException("Method-defined tokens must have a body");
        }

        private void CheckField(FieldInfo ToCheck)
        {
            if (!ToCheck.FieldType.IsAssignableFrom(typeof (string)))
                throw new InvalidCastException("Field-defined tokens must be of type string");
            if (ToCheck.GetValue(null) == null)
                throw new NoNullAllowedException("Field-defined tokens may not be null");
        }

        private void CheckForCharIgnore()
        {
            var ignore = Fields.Where(f => f.Name == SPECIAL_CHAR_IGNORE).ElementAtOrDefault(0);
            if (ignore != null)
            {
                CheckField(ignore);
                IgnoreChars = (string) ignore.GetValue(null);
            }
        }

        private void ParseTokens()
        {
            // Ordering based on PLY token ordering
            List<Token> methodTokens = new List<Token>(),
                literalTokens = new List<Token>();
            var methodNames = Methods.Select(m => m.Name).ToList();
            var fieldNames = Fields.Select(f => f.Name).ToList();

            foreach (var tknName in TokenList)
            {
                var defName = TOKEN_PREFIX + tknName;
                var ignoreName = TOKEN_PREFIX + IGNORE_PREFIX + tknName;
                if (methodNames.Contains(defName))
                {
                    methodNames.Remove(defName);
                    var method = Methods.Where(m => m.Name == defName).ElementAt(0);
                    CheckMethod(method);
                    methodTokens.Add(new Token()
                    {
                        Name = tknName,
                        Matcher = method.GetCustomAttribute<MatchString>().Matcher,
                        OnMatch = new Func<LexerToken, LexerToken>(tk => (LexerToken)method.Invoke(null, BindingFlags.Default, null, new []{tk}, null))
                    });
                }
                else if (fieldNames.Contains(defName) || fieldNames.Contains(ignoreName))
                {
                    fieldNames.Remove(defName);
                    fieldNames.Remove(ignoreName);
                    var field = Fields.Where(f => f.Name == defName || f.Name == ignoreName).ElementAt(0);
                    CheckField(field);
                    literalTokens.Add(new Token()
                    {
                        Name = tknName,
                        Matcher = (string) field.GetValue(null),
                        Ignore = fieldNames.Contains(ignoreName)
                    });
                }
                else
                {
                    throw new MissingFieldException("Cannot find the following token definition: " + defName);
                }
            }
            foreach (var leftoverMethodName in methodNames)
            {
                if (leftoverMethodName.StartsWith(TOKEN_PREFIX))
                {
                    var method = Methods.Where(m => m.Name == leftoverMethodName).ElementAt(0);
                    CheckMethod(method);
                    methodTokens.Add(new Token()
                    {
                        Name = leftoverMethodName.Slice(2),
                        Matcher = method.GetCustomAttribute<MatchString>().Matcher,
                        OnMatch = new Func<LexerToken, LexerToken>(tk => (LexerToken)method.Invoke(null, BindingFlags.Default, null, new []{tk}, null)),
                        Ignore = true
                    });
                }
            }
            methodTokens.AddRange(literalTokens.OrderByDescending(tk => tk.Matcher.Length));
            Tokens = methodTokens.ToArray();

            ReportItems.Add("Tokens: " + ", ".Combine(Tokens.Select(t => t.ToString()).ToArray()));
        }

        private void CheckForLiterals()
        {
            var literals = Fields.Where(f => f.Name == SPECIAL_CHAR_LITERALS).ElementAtOrDefault(0);
            if (literals != null)
            {
                List<Token> curTokens = Tokens.ToList();
                foreach (char c in (string) literals.GetValue(null))
                {
                    curTokens.Add(new Token() {Name = c.ToString(), Matcher = @"\" + c});
                }
            }
        }

        private void CheckForErrorHandler()
        {
            var errorHandler = Methods.Where(m => m.Name == SPECIAL_ERROR).ElementAtOrDefault(0);
            if (errorHandler != null)
            {
                this.ErrorHandler =
                    new Action<LexerToken>(
                        err_tkn => errorHandler.Invoke(null, BindingFlags.Default, null, new[] {err_tkn}, null));
            }
        }

        private void FinalizeMapper()
        {
            Mapper = new List<Tuple<Regex, Token>>();
            foreach (var tkn in Tokens)
            {
                Mapper.Add(new Tuple<Regex, Token>(tkn.Matcher == null ? null : new Regex(tkn.Matcher, RegexOptions.Compiled), tkn));
            }
        }

        public string Report(string EOL = "\r\n")
        {
            return String.Format(EOL.Combine(this.ReportItems.ToArray()));
        }

        public IEnumerable<LexerToken> Input(string Text)
        {
            this["LineNo"] = 0;
            this["LexPos"] = 0;
            this["LexData"] = Text;
            this["LexMatch"] = null;

            Text = new string(Text.Where(c => !IgnoreChars.Contains(c)).ToArray());
            while (Text.Length > (int)this["LexPos"])
            {
                bool success = false;
                foreach (var tup in Mapper)
                {
                    var curPos = (int) this["LexPos"];
                    var reg = tup.Item1;
                    if (reg == null)
                        continue;
                    var mtch = reg.Match(Text, curPos);
                    if (mtch.Success && mtch.Index == curPos)
                    {
                        var tkn = tup.Item2;
                        if (!tkn.Ignore)
                        {
                            this["LexMatch"] = mtch.Value;
                            this["LexPos"] = mtch.Length + (int) this["LexPos"];
                            yield return
                                new LexerToken()
                                {
                                    Lexer = this,
                                    LexPos = (int) this["LexPos"],
                                    LineNo = (int) this["LineNo"],
                                    Type = tkn.Name,
                                    Value = mtch.Value
                                };
                        }
                        success = true;
                        break;
                    }
                }
                if (!success)
                {
                    if (ErrorHandler != null)
                        ErrorHandler(new LexerToken()
                        {
                            LexPos = (int) this["LexPos"],
                            LineNo = (int) this["LineNo"],
                            Type = "<ERROR>",
                            Value = Text.Slice((int) this["LexPos"])
                        });
                    break;
                }
            }
        }

        public object this[string Key]
        {
            get { return PublicProperties[Key]; }
            set
            {
                if (PublicProperties.ContainsKey(Key))
                    PublicProperties[Key] = value;
                else
                    PublicProperties.Add(Key, value);
            }

            
        }


    }
}
