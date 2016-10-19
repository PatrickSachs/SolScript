using System.Text;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Library {
    [SolLibraryClass("std", TypeDef.TypeMode.Singleton)]
    [SolLibraryName("Table")]
    [UsedImplicitly]
    public class TableModule {
        // ReSharper disable InconsistentNaming
        [UsedImplicitly]
        public SolNumber append(SolTable table, SolValue value) {
            return table.Append(value);
        }
        
        [UsedImplicitly]
        public int get_n(SolTable table) {
            return table.Count;
        }

        [UsedImplicitly]
        public string concat(SolTable table, [CanBeNull] string separator) {
            StringBuilder builder = new StringBuilder();
            foreach (var pair in table) {
                if (separator != null && builder.Length != 0) {
                    builder.Append(separator);
                }
                builder.Append(pair.Value);
            }
            return builder.ToString();
        }

        // ReSharper restore InconsistentNaming
    }
}