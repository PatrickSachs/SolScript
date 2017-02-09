using System;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeStringMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_HIGH;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type == typeof(string);
        }

        /// <inheritdoc />
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            return new SolString((string) value);
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolString.TYPE, true);
        }

        #endregion
    }
}