using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library {
    [SolLibraryClass("std", TypeDef.TypeMode.Singleton)]
    [SolLibraryName("String")]
    [UsedImplicitly]
    public class StringModule {
        // ReSharper disable InconsistentNaming
        [UsedImplicitly] public bool use_local_culture;

        [UsedImplicitly]
        public SolString empty => SolString.Empty;

        [UsedImplicitly]
        public string to_lower(string value) {
            return value.ToLower(use_local_culture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture);
        }

        [UsedImplicitly]
        public string to_upper(string value) {
            return value.ToUpper(use_local_culture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture);
        }

        [UsedImplicitly]
        public string char_at(SolExecutionContext context, string value, int index) {
            if (index > value.Length) {
                throw new SolScriptInterpreterException(context.CurrentLocation + " : Cannot get char at index " + index +
                                                        " from a string with a length of " + value.Length +
                                                        "! (String: " + value + ")");
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
        public string skip(string value, int amount) {
            if (amount > value.Length) {
                throw new SolScriptInterpreterException("Cannot skip " + amount + " elements of a string of length" +
                                                        value.Length + ".");
            }
            return value.Substring(amount, value.Length - amount);
        }

        [UsedImplicitly]
        public string take(string value, int amount) {
            if (amount > value.Length) {
                throw new SolScriptInterpreterException("Cannot take " + amount + " elements from a string of length" +
                                                        value.Length + ".");
            }
            return value.Substring(0, amount);
        }

        [UsedImplicitly]
        public string substring(string value, int start, int amount) {
            if (start + amount > value.Length) {
                throw new SolScriptInterpreterException("Cannot substring " + start + "+" + amount +
                                                        " elements from a string of length" + value.Length + ".");
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
                return SolBoolean.ValueOf(parsed);
            }
            return SolNil.Instance;
        }

        public SolValue regex(string pattern, string input) {
            Match match = new Regex(pattern).Match(input);
            if (!match.Success) {
                return SolNil.Instance;
            }
            SolTable matchTable = new SolTable();
            foreach (Group mGrp in match.Groups)
            {
                SolTable grpTable = new SolTable {
                    [new SolString("index")] = new SolNumber(mGrp.Index),
                    [new SolString("length")] = new SolNumber(mGrp.Length),
                    [new SolString("value")] = new SolString(mGrp.Value)
                };
                matchTable.Append(grpTable);
            }
            return matchTable;
        }

        // ReSharper restore InconsistentNaming
    }
}