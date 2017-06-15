using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Exceptions;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;
using SolScript.Utility;

// ReSharper disable InconsistentNaming

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     Provides some helper methods related to <see cref="SolString" />s.
    /// </summary>
    [SolTypeDescriptor(std.NAME, SolTypeMode.Singleton, typeof(std_String)), SolLibraryName(TYPE), PublicAPI]
    public class std_String
    {
        [SolLibraryVisibility(std.NAME, true)]
        private std_String()
        {
            UseLocalCulture = true;
        }

        /// <summary>
        ///     The type name is "String".
        /// </summary>
        [SolLibraryVisibility(std.NAME, false)]
        public const string TYPE = "String";

        private static readonly SolString Str_index = SolString.ValueOf("index").Intern();
        private static readonly SolString Str_length = SolString.ValueOf("length").Intern();
        private static readonly SolString Str_value = SolString.ValueOf("value").Intern();

        /// <inheritdoc cref="SolString.Empty" />
        [SolContract(SolString.TYPE, false)]
        public SolString empty => SolString.Empty;

        /// <summary>
        ///     Should the local culture be used for string operations? (Default: true)
        /// </summary>
        [SolContract(SolBool.TYPE, false)]
        public SolBool use_local_culture {
            get { return SolBool.ValueOf(UseLocalCulture); }
            set { UseLocalCulture = value.Value; }
        }

        /// <inheritdoc cref="use_local_culture" />
        [SolLibraryVisibility(std.NAME, false)]
        public static bool UseLocalCulture { get; set; }

        #region Overrides

        public override string ToString()
        {
            return "String Module";
        }

        #endregion

        /// <summary>
        ///     Checks if the given <see cref="SolString" /> is in lower case.
        /// </summary>
        /// <param name="value">The <see cref="SolString" /> to check.</param>
        /// <returns>true if the <see cref="SolString" /> is in lower case, false if not.</returns>
        [SolContract(SolBool.TYPE, false)]
        public SolBool is_lower([SolContract(SolString.TYPE, false)] SolString value)
        {
            foreach (char c in value.Value) {
                if (!char.IsLower(c)) {
                    return SolBool.False;
                }
            }
            return SolBool.True;
        }

        /// <summary>
        ///     Converts a <see cref="SolString" /> into lower case.
        /// </summary>
        /// <param name="value">The <see cref="SolString" /> to convert.</param>
        /// <returns>The lower case version of <paramref name="value" />.</returns>
        [SolContract(SolString.TYPE, false)]
        public SolString to_lower([SolContract(SolString.TYPE, false)] SolString value)
        {
            return SolString.ValueOf(value.Value.ToLower(UseLocalCulture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Checks if the given <see cref="SolString" /> is in upper case.
        /// </summary>
        /// <param name="value">The <see cref="SolString" /> to check.</param>
        /// <returns>true if the <see cref="SolString" /> is in upper case, false if not.</returns>
        [SolContract(SolBool.TYPE, false)]
        public SolBool is_upper([SolContract(SolString.TYPE, false)] SolString value)
        {
            foreach (char c in value.Value) {
                if (!char.IsUpper(c)) {
                    return SolBool.False;
                }
            }
            return SolBool.True;
        }

        /// <summary>
        ///     Converts a <see cref="SolString" /> into upper case.
        /// </summary>
        /// <param name="value">The <see cref="SolString" /> to convert.</param>
        /// <returns>The upper case version of <paramref name="value" />.</returns>
        [SolContract(SolString.TYPE, false)]
        public SolString to_upper([SolContract(SolString.TYPE, false)] SolString value)
        {
            return SolString.ValueOf(value.Value.ToUpper(UseLocalCulture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture));
        }

        /// <summary>
        ///     Gets a character at a given index of a <see cref="SolString" />.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value">The <see cref="SolString" /> to get the character of.</param>
        /// <param name="index">(Must be integer) The character index.</param>
        /// <returns>The character.</returns>
        /// <exception cref="SolRuntimeException">Index out of bounds or not an integer.</exception>
        [SolContract(SolString.TYPE, false)]
        public SolString char_at(SolExecutionContext context, [SolContract(SolString.TYPE, false)] SolString value, [SolContract(SolNumber.TYPE, false)] SolNumber index)
        {
            int intIndex;
            if (!InternalHelper.NumberToInteger(index, out intIndex)) {
                throw new SolRuntimeException(context, "Tried to access index " + index + " - The index has a decimal part.");
            }
            if (intIndex >= value.Value.Length || intIndex < 0) {
                throw new SolRuntimeException(context, "Tried to access index " + index + " - The string only has a length of " + value.Value.Length + ".");
            }
            return SolString.ValueOf(new string(value.Value[intIndex], 1));
        }

        /// <summary>
        ///     Converts a <see cref="SolString" /> into a character table.
        /// </summary>
        /// <param name="value">The <see cref="SolString" /> to convert.</param>
        /// <param name="ids">
        ///     (Optional) If this is true the character ids will be added(<see cref="SolNumber" />) to the table
        ///     instead of the characters a strings(<see cref="SolString" />). (Default: false)
        /// </param>
        /// <returns>The <see cref="SolTable" /> containg the characters.</returns>
        [SolContract(SolTable.TYPE, false)]
        public SolTable to_char_table([SolContract(SolString.TYPE, false)] SolString value, [SolContract(SolBool.TYPE, true)] SolBool ids)
        {
            bool asIds = ids?.Value ?? false;
            SolTable table = new SolTable();
            foreach (char chr in value.Value) {
                if (asIds) {
                    table.Append(new SolNumber(chr));
                } else {
                    table.Append(SolString.ValueOf(new string(chr, 1)));
                }
            }
            return table;
        }

        /// <summary>
        ///     Tries to find the position of an occurence of a certain <paramref name="term" /> in a <see cref="SolString" />(
        ///     <paramref name="value" />).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value">The <see cref="SolString" /> to search.</param>
        /// <param name="term">
        ///     The <see cref="SolString" /> string that should be contained in <paramref name="value" /> in order
        ///     to produce a match.
        /// </param>
        /// <param name="start">(Optional)(Must be integer) The start index. (Default: 0)</param>
        /// <returns>The index of the position of the first occurence. Or -1 if none.</returns>
        /// <exception cref="SolRuntimeException">Invalid index(<paramref name="start" />).</exception>
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber find(SolExecutionContext context, [SolContract(SolString.TYPE, false)] SolString value, [SolContract(SolString.TYPE, false)] SolString term,
            [SolContract(SolNumber.TYPE, true)] SolNumber start)
        {
            int intStart = 0;
            if (start != null && !InternalHelper.NumberToInteger(start, out intStart)) {
                throw new SolRuntimeException(context, "Tried to access index " + start + " - The index has a decimal part.");
            }
            if (intStart >= value.Value.Length || intStart < 0) {
                throw new SolRuntimeException(context, "Tried to access index " + start + " - The string only has a length of " + value.Value.Length + ".");
            }
            return new SolNumber(value.Value.IndexOf(term.Value, intStart, UseLocalCulture ? StringComparison.CurrentCulture : StringComparison.Ordinal));
        }

        /// <summary>
        ///     Formats the given <see cref="SolString" />(<paramref name="value" />) with the given <paramref name="arguments" />.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value">The <see cref="SolString" /> to format.</param>
        /// <param name="arguments">The arguments to use as formatting members.</param>
        /// <returns>The formatted <see cref="SolString" />.</returns>
        /// <remarks>
        ///     The formatting is applied in the style of <c>{0}</c> for the first argument, <c>{1}</c> for the second, and so
        ///     on.
        /// </remarks>
        /// <exception cref="SolRuntimeException">
        ///     Invalid <paramref name="value" /> format(e.g. <paramref name="arguments" />
        ///     length mismatch).
        /// </exception>
        [SolContract(SolString.TYPE, false)]
        public SolString format(SolExecutionContext context, [SolContract(SolString.TYPE, false)] SolString value, [SolContract(SolTable.TYPE, true), CanBeNull]  params SolValue[] arguments)
        {
            if (arguments == null) {
                return value;
            }
            try {
                return string.Format(value.Value, arguments);
            } catch (FormatException ex) {
                throw new SolRuntimeException(context, "Invalid string format.", ex);
            }
        }

        /// <summary>
        ///     Repeats the given <paramref name="value" /> by <paramref name="amount" /> times.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="value">The <see cref="SolString" /> to repeat.</param>
        /// <param name="amount">(Must be integer) How many times the <paramref name="value" /> should be repeated.</param>
        /// <returns>The repeated <see cref="SolString" />.</returns>
        /// <exception cref="SolRuntimeException">
        ///     Invalid <paramref name="amount" /> or internal buffer overflow(Too high
        ///     <paramref name="amount" />).
        /// </exception>
        [SolContract(SolString.TYPE, false)]
        public SolString repeat(SolExecutionContext context, [SolContract(SolString.TYPE, false)] SolString value, [SolContract(SolNumber.TYPE, false)] SolNumber amount)
        {
            int intAmount;
            if (!InternalHelper.NumberToInteger(amount, out intAmount)) {
                throw new SolRuntimeException(context, "Tried to repeat the string " + amount + " times - The number has a decimal part.");
            }
            if (intAmount < 0) {
                throw new SolRuntimeException(context, "Tried to repeat the string " + intAmount + " times - The number is smaller than zero.");
            }
            if (intAmount == 1) {
                return value;
            }
            try {
                return new StringBuilder(value.Value.Length * intAmount).Insert(0, value.Value, intAmount).ToString();
            } catch (OutOfMemoryException ex) {
                throw new SolRuntimeException(context, "Tried to repeat the string " + intAmount + " times - The sequence is too long for the internal string buffer.", ex);
            }
        }

        /// <summary>
        ///     Reverses the given <see cref="SolString" />.
        /// </summary>
        /// <param name="value">The <see cref="SolString" /> to reverse.</param>
        /// <returns>The reversed version of <paramref name="value" />.</returns>
        [SolContract(SolString.TYPE, false)]
        public SolString reverse([SolContract(SolString.TYPE, false)] SolString value)
        {
            char[] array = value.Value.ToCharArray();
            // ReSharper disable once ExceptionNotDocumented
            // The array is never multidimensional.
            Array.Reverse(array);
            return SolString.ValueOf(new string(array));
        }

        [UsedImplicitly]
        public string skip(SolExecutionContext context, string value, int amount)
        {
            if (amount > value.Length) {
                throw new SolRuntimeException(context, "Tried to skip " + amount + " characters in string \"" + value + "\"(Length: " + value.Length + ").");
            }
            return value.Substring(amount, value.Length - amount);
        }

        [UsedImplicitly]
        public string take(SolExecutionContext context, string value, int amount)
        {
            if (amount > value.Length) {
                throw new SolRuntimeException(context, "Tried to take " + amount + " characters in string \"" + value + "\"(Length: " + value.Length + ").");
            }
            return value.Substring(0, amount);
        }

        [UsedImplicitly]
        public string substring(SolExecutionContext context, string value, int start, int amount)
        {
            int checkIdx = start + amount;
            if (checkIdx > value.Length || start < 0 || amount < 0) {
                throw new SolRuntimeException(context, "Tried to substring " + amount + " characters starting at index " + start + " in string \"" + value + "\"(Length: " + value.Length + ").");
            }
            return value.Substring(start, amount);
        }

        /// <summary>
        ///     Joins several values to a string using their string conversion function.
        /// </summary>
        /// <param name="separator">The separator placed between each value.</param>
        /// <param name="values">The values to join.</param>
        /// <returns>The joined string.</returns>
        [SolContract(SolString.TYPE, false)]
        public SolString join([SolContract(SolString.TYPE, false)] SolString separator, [SolContract(SolTable.TYPE, false)] params SolValue[] values)
        {
            return SolString.ValueOf(values.JoinToString(separator));
        }

        [UsedImplicitly]
        public SolValue parse_number(string input)
        {
            double parsed;
            if (double.TryParse(input, NumberStyles.Float, UseLocalCulture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture, out parsed)) {
                return new SolNumber(parsed);
            }
            return SolNil.Instance;
        }

        [UsedImplicitly]
        public SolValue parse_bool(string input)
        {
            bool parsed;
            if (bool.TryParse(input, out parsed)) {
                return SolBool.ValueOf(parsed);
            }
            return SolNil.Instance;
        }

        [UsedImplicitly]
        public SolTable split(string value, params string[] separators)
        {
            string[] splitResult = value.Split(separators, StringSplitOptions.None);
            SolTable table = new SolTable();
            foreach (string s in splitResult) {
                table.Append(SolString.ValueOf(s));
            }
            return table;
        }

        [SolContract("string", false)]
        public SolString intern([SolContract("string", false)] SolString value)
        {
            value.Intern();
            return value;
        }

        [UsedImplicitly]
        public SolValue regex(string pattern, string input)
        {
            Match match = new Regex(pattern).Match(input);
            if (!match.Success) {
                return SolNil.Instance;
            }
            SolTable matchTable = new SolTable();
            foreach (Group mGrp in match.Groups) {
                SolTable grpTable = new SolTable {
                    [Str_index] = new SolNumber(mGrp.Index),
                    [Str_length] = new SolNumber(mGrp.Length),
                    [Str_value] = SolString.ValueOf(mGrp.Value)
                };
                matchTable.Append(grpTable);
            }
            return matchTable;
        }
    }
}