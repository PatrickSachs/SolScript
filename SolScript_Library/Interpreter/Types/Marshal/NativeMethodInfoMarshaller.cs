using System;
using System.Reflection;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeMethodInfoMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_VERY_LOW;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type == typeof(MethodInfo);
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">No matching SolType for a parameter type.</exception>
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            return new SolNativeLamdaFunction(assembly, (MethodInfo) value, DynamicReference.NullReference.Instance);
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolFunction.TYPE, true);
        }

        #endregion
    }
}