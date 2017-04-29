// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using SolScript.Interpreter;
using SolScript.Interpreter.Library;
using SolScript.Interpreter.Types;

namespace SolScript.Libraries.std
{
    /// <summary>
    ///     The <see cref="std_Table" /> is used to provide several helper methods related to <see cref="SolTable" />s.
    /// </summary>
    [SolTypeDescriptor(std.NAME, SolTypeMode.Singleton, typeof(std_Table))]
    [SolLibraryName(TYPE)]
    [PublicAPI]
    public class std_Table
    {
        [SolLibraryVisibility(std.NAME, true)]
        private std_Table() {}

        /// <summary>
        ///     The type name is "Table".
        /// </summary>
        [SolLibraryVisibility(std.NAME, false)] public const string TYPE = "Table";

        #region Overrides

        /// <inheritdoc />
        public override string ToString()
        {
            return "Table Singleton";
        }

        #endregion

        /// <inheritdoc cref="SolTable.Append(SolValue)" />
        [SolContract(SolNumber.TYPE, false)]
        public SolNumber append([SolContract(SolTable.TYPE, false)] SolTable table, [SolContract(SolValue.ANY_TYPE, true)] SolValue value)
        {
            return table.Append(value);
        }

        /// <summary>
        ///     Concatenates all values in a <see cref="SolTable" /> to a string, seperated by an optional
        ///     <paramref name="separator" />.
        /// </summary>
        /// <param name="table">The <see cref="SolTable" /> to concatenate.</param>
        /// <param name="separator">(Optional) The seperator between each value.</param>
        /// <returns>The string representing the table.</returns>
        [SolContract(SolString.TYPE, false)]
        public SolString concat([SolContract(SolTable.TYPE, false)] SolTable table, [SolContract(SolString.TYPE, true)] [CanBeNull] SolString separator)
        {
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<SolValue, SolValue> pair in table) {
                if (separator != null && builder.Length != 0) {
                    builder.Append(separator);
                }
                builder.Append(pair.Value);
            }
            return builder.ToString();
        }

        /// <summary>
        ///     Clears all keys and values from the <see cref="SolTable" />, emptying it.
        /// </summary>
        /// <param name="table">The <see cref="SolTable" /> to clear.</param>
        /// <returns>The <see cref="SolTable" /> itself.</returns>
        [SolContract(SolTable.TYPE, false)]
        public SolTable clear([SolContract(SolTable.TYPE, false)] SolTable table)
        {
            table.Clear();
            return table;
        }
    }
}