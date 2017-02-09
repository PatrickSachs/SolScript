/*using System;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeArrayMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_DEFAULT;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type.IsArray;
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">Failed to marshal an array element.</exception>
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            Array array = (Array) value;
            SolTable table = new SolTable();
            for (int i = 0; i < array.Length; i++) {
                object iValue = array.GetValue(i);
                table.Append(SolMarshal.MarshalFromNative(assembly, iValue.GetType(), iValue));
            }
            return table;
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolTable.TYPE, true);
        }

        #endregion
    }
}*/