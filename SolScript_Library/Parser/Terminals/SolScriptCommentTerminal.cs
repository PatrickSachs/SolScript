﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using Irony.Parsing;

namespace SolScript.Parser.Terminals
{
    internal class SolScriptCommentTerminal : Terminal
    {
        public SolScriptCommentTerminal(string name)
            : base(name, TokenCategory.Comment)
        { 
            //assign max priority
            Priority = TerminalPriority.High;
        }

        private const string START_SYMBOL = "//";

        #region overrides
        public override void Init(GrammarData grammarData)
        {
            base.Init(grammarData);
            SetFlag(TermFlags.IsMultiline);

            if (this.EditorInfo == null)
            {
                this.EditorInfo = new TokenEditorInfo(TokenType.Comment, TokenColor.Comment, TokenTriggers.None);
            }
        }
        
        public override Token TryMatch(ParsingContext context, ISourceStream source)
        {
            Token result;
            if (context.VsLineScanState.Value != 0)
            {
                byte commentLevel = context.VsLineScanState.TokenSubType;
                result = CompleteMatch(context, source, commentLevel);
            }
            else
            {
                //we are starting from scratch
                byte commentLevel = 0;
                if (!BeginMatch(context, source, ref commentLevel))
                    return null;

                result = CompleteMatch(context, source, commentLevel);
            }

            if (result != null)
                return result;

            if (context.Mode == ParseMode.VsLineScan)
                return CreateIncompleteToken(context, source);

            return source.CreateToken(new Terminal("Unclosed comment block"));
        }

        private Token CreateIncompleteToken(ParsingContext context, ISourceStream source)
        {
            source.PreviewPosition = source.Text.Length;
            Token result = source.CreateToken(this.OutputTerminal);
            result.Flags |= TokenFlags.IsIncomplete;
            context.VsLineScanState.TerminalIndex = this.MultilineIndex;
            return result;
        }

        private bool BeginMatch(ParsingContext context, ISourceStream source, ref byte commentLevel)
        {
            if (source.MatchSymbol("/*")) {
                commentLevel = 1;
            } else if (!source.MatchSymbol("//")) {
                commentLevel = 0;
                return false;
            }

            //Increment position of comment so we don't rescan the same text.
            source.PreviewPosition += START_SYMBOL.Length + commentLevel;

            return true;
        }

        private Token CompleteMatch(ParsingContext context, ISourceStream source, byte commentLevel)
        {
            if (commentLevel == 0)
            {
                var line_breaks = new char[] { '\n', '\r', '\v' };
                var firstCharPos = source.Text.IndexOfAny(line_breaks, source.PreviewPosition);
                if (firstCharPos > 0)
                {
                    source.PreviewPosition = firstCharPos;
                }
                else
                {
                    source.PreviewPosition = source.Text.Length;
                }

                return source.CreateToken(this.OutputTerminal);
            }

            while (!source.EOF())
            {
                string text = source.Text.Substring(source.PreviewPosition);
                //var matches = Regex.Matches(text, @"\](=*)\]");
                var matches = Regex.Matches(text, @"\*/");
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Value.Length == (int)commentLevel - 1)
                    {
                        source.PreviewPosition += match.Index + match.Length;

                        if (context.VsLineScanState.Value != 0)
                        {
                            //We are using line-mode and begin terminal was on previous line.
                            SourceLocation tokenStart = new SourceLocation();
                            tokenStart.Position = 0;

                            string lexeme = source.Text.Substring(0, source.PreviewPosition);

                            context.VsLineScanState.Value = 0;
                            return new Token(this, tokenStart, lexeme, null);
                        }
                        else
                        {
                            return source.CreateToken(this.OutputTerminal);
                        }
                    }
                }

                source.PreviewPosition++;
            }
            //The full match wasn't found, store the state for future parsing.
            //   context.VsLineScanState.TerminalIndex = this.MultilineIndex;
            context.VsLineScanState.TokenSubType = commentLevel;
            return null;
        }

        public override IList<string> GetFirsts()
        {
            return new string[] { START_SYMBOL };
        }
        #endregion
    }
}


