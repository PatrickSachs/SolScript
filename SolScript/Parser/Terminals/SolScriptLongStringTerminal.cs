using System.Collections.Generic;
using System.Text.RegularExpressions;
using Irony.Parsing;
using SolScript.Interpreter;

namespace SolScript.Parser.Terminals
{
    internal class SolScriptLongStringTerminal : Terminal
    {
        public SolScriptLongStringTerminal(string name) : base(name, TokenCategory.Content) {}

        public readonly string StartSymbol = "[";

        #region overrides

        public override void Init(GrammarData grammarData)
        {
            base.Init(grammarData);
            SetFlag(TermFlags.IsMultiline);
            if (EditorInfo == null) {
                EditorInfo = new TokenEditorInfo(TokenType.String, TokenColor.String, TokenTriggers.None);
            }
        }

        public override Token TryMatch(ParsingContext context, ISourceStream source)
        {
            Token result;
            if (context.VsLineScanState.Value != 0) {
                byte level = context.VsLineScanState.TokenSubType;
                result = CompleteMatch(context, source, level);
            } else {
                //we are starting from scratch
                byte level = 0;
                if (!BeginMatch(context, source, ref level)) {
                    return null;
                }
                result = CompleteMatch(context, source, level);
            }
            if (result != null) {
                return result;
            }
            if (context.Mode == ParseMode.VsLineScan) {
                return CreateIncompleteToken(context, source);
            }
            return source.CreateToken(new Terminal("Unclosed comment block"));
        }

        private Token CreateIncompleteToken(ParsingContext context, ISourceStream source)
        {
            source.PreviewPosition = source.Text.Length;
            Token result = source.CreateToken(OutputTerminal);
            result.Flags |= TokenFlags.IsIncomplete;
            context.VsLineScanState.TerminalIndex = MultilineIndex;
            return result;
        }

        private bool BeginMatch(ParsingContext context, ISourceStream source, ref byte level)
        {
            //Check starting symbol
            if (!source.MatchSymbol(StartSymbol)) {
                return false;
            }
            //Found starting --, now determine whether this is a long comment.
            string text = source.Text.Substring(source.PreviewPosition + StartSymbol.Length);
            Match match = Regex.Match(text, @"^(=*)\[");
            if (match.Value != string.Empty) {
                level = (byte) match.Groups[1].Value.Length;
                return true;
            }
            return false;
        }

        private Token CompleteMatch(ParsingContext context, ISourceStream source, byte level)
        {
            string text = source.Text.Substring(source.PreviewPosition);
            MatchCollection matches = Regex.Matches(text, @"\](=*)\]");
            foreach (Match match in matches) {
                int grLength = match.Groups[1].Value.Length;
                if (grLength == level) {
                    source.PreviewPosition += match.Index + match.Length;
                    if (context.VsLineScanState.Value != 0) {
                        //We are using line-mode and begin terminal was on previous line.
                        SourceLocation tokenStart = new SourceLocation(SolSourceLocation.NATIVE_FILE, 0, 0, 0);
                        string lexeme = source.Text.Substring(0, source.PreviewPosition);
                        context.VsLineScanState.Value = 0;
                        return new Token(this, tokenStart, lexeme, lexeme.Substring(grLength + 2, lexeme.Length - (grLength + 2) * 2));
                    }
                    Token token = source.CreateToken(OutputTerminal);
                    // Skip the first character if it is a line break. (Or two if the file uses windows line breaks...)
                    char startChar = token.Text[level + 2];
                    int skip = 0;
                    if (startChar == '\n') {
                        skip++;
                        // Im certain that some weird OS out there does it this way! 
                        // This even makes some sort of sense...
                        if (token.Text[level + 3] == '\r') {
                            skip++;
                        }
                    }
                    if (startChar == '\r') {
                        skip++;
                        // Hello windows, my old friend.
                        if (token.Text[level + 3] == '\n') {
                            skip++;
                        }
                    }
                    token.Value = token.Text.Substring(level + 2 + skip, token.Text.Length - (level + 2) * 2 - skip);
                    return token;
                }
            }

            //The full match wasn't found, store the state for future parsing.
            context.VsLineScanState.TerminalIndex = MultilineIndex;
            context.VsLineScanState.TokenSubType = level;
            return null;
        }

        public override IList<string> GetFirsts()
        {
            return new[] {StartSymbol};
        }

        #endregion
    }
}