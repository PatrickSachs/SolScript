using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeDictionaryMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => NativeEnumerableMarshaller.PRIORITY + 5;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type.GetInterfaces().Contains(typeof(IDictionary));
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
            IDictionary dictionary = (IDictionary) value;
            IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
            SolTable table = new SolTable();
            while (enumerator.MoveNext()) {
                SolValue sKey = SolMarshal.MarshalFromNative(assembly, enumerator.Key);
                SolValue sValue = SolMarshal.MarshalFromNative(assembly, enumerator.Value);
                table[sKey] = sValue;
            }
            return table;
        }

        #endregion
    }
}