using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library.Classes {
    [SolLibraryClass("std", SolTypeMode.Singleton)]
    [SolLibraryName("String")]
    [UsedImplicitly]
    public class StringModule {
        // ReSharper disable InconsistentNaming
        [UsedImplicitly] public bool use_local_culture;

        [UsedImplicitly]
        public SolString empty => SolString.Empty;

        [UsedImplicitly]
        public bool is_lower(string value) {
            foreach (char c in value) {
                if (!char.IsLower(c)) {
                    return false;
                }
            }
            return true;
        }

        [UsedImplicitly]
        public string to_lower(string value) {
            return value.ToLower(use_local_culture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture);
        }

        [UsedImplicitly]
        public bool is_upper(string value) {
            foreach (char c in value) {
                if (!char.IsUpper(c)) {
                    return false;
                }
            }
            return true;
        }

        [UsedImplicitly]
        public string to_upper(string value) {
            return value.ToUpper(use_local_culture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture);
        }

        [UsedImplicitly]
        public string char_at(SolExecutionContext context, string value, int index) {
            if (index > value.Length) {
                throw SolScriptInterpreterException.IndexOutOfRange(context, index.ToString(), "0",
                    value.Length.ToString(), "Tried to access invalid index in string \"" + value + "\".");
            }
            return new string(value[index], 1);
        }

        [UsedImplicitly]
        public SolTable to_char_table(string value, bool? ids) {
            bool asIds = ids ?? false;
            SolTable table = new SolTable();
            foreach (char chr in value) {
                if (asIds) {
                    table.Append(new SolNumber(chr));
                } else {
                    table.Append(new SolString(new string(chr, 1)));
                }
            }
            return table;
        }

        [UsedImplicitly]
        public int find(string value, string term, int? start) {
            return value.IndexOf(term, start ?? 0,
                use_local_culture ? StringComparison.CurrentCulture : StringComparison.Ordinal);
        }

        [UsedImplicitly]
        public string format(string value, params SolValue[] args) {
            return string.Format(value, args);
        }

        [UsedImplicitly]
        public string repeat(string value, int amount) {
            return new StringBuilder(value.Length*amount).Insert(0, value, amount).ToString();
        }

        [UsedImplicitly]
        public string reverse(string value) {
            var array = value.ToCharArray();
            Array.Reverse(array);
            return new string(array);
        }

        [UsedImplicitly]
        public string skip(SolExecutionContext context, string value, int amount) {
            if (amount > value.Length) {
                throw SolScriptInterpreterException.IndexOutOfRange(context, amount.ToString(), "0",
                    value.Length.ToString(), "Tried to skip to an invalid string index.");
            }
            return value.Substring(amount, value.Length - amount);
        }

        [UsedImplicitly]
        public string take(SolExecutionContext context, string value, int amount) {
            if (amount > value.Length)
            {
                throw SolScriptInterpreterException.IndexOutOfRange(context, (value.Length - amount).ToString(), "0",
                    value.Length.ToString(), "Tried to take to an invalid string index.");
            }
            return value.Substring(0, amount);
        }

        public override string ToString()
        {
            return "String Module";
        }
        [UsedImplicitly]
        public string substring(SolExecutionContext context, string value, int start, int amount) {
            int checkIdx = start + amount;
            if (checkIdx > value.Length || start < 0 || amount < 0)
            {
                throw SolScriptInterpreterException.IndexOutOfRange(context, start + "\"-\"" + checkIdx, "0",
                    value.Length.ToString(), "Tried to substring invalid indices from a string.");
            }
            return value.Substring(start, amount);
        }

        [UsedImplicitly]
        public string join(string separator, params SolValue[] values) {
            return string.Join(separator, (object[]) values);
        }

        [UsedImplicitly]
        public SolValue parse_number(string input) {
            double parsed;
            if (double.TryParse(input, NumberStyles.Float,
                use_local_culture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture, out parsed)) {
                return new SolNumber(parsed);
            }
            return SolNil.Instance;
        }

        [UsedImplicitly]
        public SolValue parse_bool(string input) {
            bool parsed;
            if (bool.TryParse(input, out parsed)) {
                return SolBool.ValueOf(parsed);
            }
            return SolNil.Instance;
        }

        [UsedImplicitly]
        public SolTable split(string value, params string[] separators) {
            var splitResult = value.Split(separators, StringSplitOptions.None);
            SolTable table = new SolTable();
            foreach (string s in splitResult) {
                table.Append(new SolString(s));
            }
            return table;
        }

        private static readonly SolString s_RegexIndex = new SolString("index");
        private static readonly SolString s_RegexLength = new SolString("length");
        private static readonly SolString s_RegexValue = new SolString("value");

        [UsedImplicitly]
        public SolValue regex(string pattern, string input) {
            Match match = new Regex(pattern).Match(input);
            if (!match.Success) {
                return SolNil.Instance;
            }
            SolTable matchTable = new SolTable();
            foreach (Group mGrp in match.Groups) {
                SolTable grpTable = new SolTable {
                    [s_RegexIndex] = new SolNumber(mGrp.Index),
                    [s_RegexLength] = new SolNumber(mGrp.Length),
                    [s_RegexValue] = new SolString(mGrp.Value)
                };
                matchTable.Append(grpTable);
            }
            return matchTable;
        }

        // ReSharper restore InconsistentNaming
    }
}