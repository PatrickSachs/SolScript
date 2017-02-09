using System;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeGenericAutoDelegateMarshaller : ISolNativeMarshaller
    {
        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_DEFAULT;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SolFunction.AutoDelegate<>);
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return new SolType(SolFunction.TYPE, true);
        }

        /// <inheritdoc />
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            Type returnType = type.GetGenericArguments()[0];
            Type funcType = typeof(SolGenericAutoDelgateWrapperFunction<>).MakeGenericType(returnType);
            return (SolFunction) Activator.CreateInstance(funcType, assembly, value);
        }

        #endregion
    }
}