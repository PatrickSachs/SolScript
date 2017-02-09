using System;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeVoidMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_VERY_LOW;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type == typeof(void);
        }

        /// <inheritdoc />
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            return SolNil.Instance;
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolNil.TYPE, true);
        }

        #endregion
    }
}