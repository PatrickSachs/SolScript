using System;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeFunctionDelegateMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_DEFAULT;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type == typeof(SolFunction.DirectDelegate);
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolFunction.TYPE, true);
        }

        /// <inheritdoc />
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            return new SolScriptDelegateWrapperFunction(assembly, (SolFunction.DirectDelegate) value);
        }

        #endregion
    }
}