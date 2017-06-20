using System;
using System.Reflection;
using SolScript.Exceptions;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Types.Marshal
{
    /// <summary>
    ///     This marshaller converts <see cref="MethodInfo" /> objects to <see cref="SolNativeLamdaFunction" />s.
    /// </summary>
    /// <remarks>They can only be invoked on null references.</remarks>
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
            return SolNativeLamdaFunction.CreateFrom(assembly, (MethodInfo) value, DynamicReference.NullReference.Instance, null);
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolFunction.TYPE, true);
        }

        #endregion
    }
}