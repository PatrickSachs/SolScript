using System;
using System.Collections;
using System.Linq;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeEnumerableMarshaller : ISolNativeMarshaller
    {
        public const int PRIORITY = SolMarshal.PRIORITY_LOW;

        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => PRIORITY;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type.GetInterfaces().Contains(typeof(IEnumerable));
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolTable.TYPE, true);
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">Failed to marshal the given value.</exception>
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            IEnumerable enumerable = (IEnumerable) value;
            SolTable table = new SolTable();
            foreach (object enumerableValue in enumerable) {
                table.Append(SolMarshal.MarshalFromNative(assembly, enumerableValue?.GetType() ?? typeof(object), enumerableValue));
            }
            return table;
        }

        #endregion
    }
}