﻿using Irony.Parsing;

namespace SolScript.Parser.Literals {
    public class SolScriptStringLiteral : StringLiteral {
        public SolScriptStringLiteral(string name)
            : base(name) {
            AddStartEnd("'", StringOptions.AllowsAllEscapes);
            AddStartEnd("\"", StringOptions.AllowsAllEscapes);
        }

        protected override bool ReadBody(ISourceStream source, CompoundTokenDetails details) {
            /*int nlPos = source.Text.IndexOf('\n', source.PreviewPosition);
            if (source.Text[nlPos - 1] == '\\')
                details.Flags += (short) StringOptions.AllowsLineBreak;*/
                
            return base.ReadBody(source, details);
        }

        protected override string HandleSpecialEscape(string segment, CompoundTokenDetails details) {
            if (string.IsNullOrEmpty(segment)) return string.Empty;
            char first = segment[0];
            switch (first) {
                case 'a':
                case 'b':
                case 'f':
                case 'n':
                case 'r':
                case 't':
                case 'v':
                case '\\':
                case '"':
                case '\'':
                    break;

                case '0':
                case '1':
                case '2': {
                    bool success = false;
                    if (segment.Length >= 3) {
                        //Verify that a numeric escape is 3 characters
                        string value = segment.Substring(0, 3);
                        int dummy = 0;
                        success = int.TryParse(value, out dummy);
                    }

                    if (!success)
                        details.Error = "Invalid escape sequence: \000 must be a valid number.";
                }
                    break;
            }
            details.Error = "Invalid escape sequence: \\" + segment;
            return segment;
        }
    }
}