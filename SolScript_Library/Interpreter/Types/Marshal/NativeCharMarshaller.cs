using System;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeCharMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_DEFAULT;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type == typeof(char);
        }

        /// <inheritdoc />
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            return new SolString(new string((char) value, 1));
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolString.TYPE, false);
        }

        #endregion
    }
}