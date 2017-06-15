using System;
using System.Reflection;
using SolScript.Exceptions;

namespace SolScript.Interpreter.Types.Marshal
{
    public class NativeNullableMarshaller : ISolNativeMarshaller
    {
        //private readonly PropertyInfo m_HasValue = typeof(Nullable<>).GetProperty("HasValue", BindingFlags.Public | BindingFlags.Instance);
        //private readonly PropertyInfo m_Value = typeof(Nullable<>).GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);

        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => SolMarshal.PRIORITY_LOW;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            if (type.IsGenericType) {
                return type.GetGenericTypeDefinition() == typeof(Nullable<>);
            }
            return false;
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">Failed to marshal the wrapped value.</exception>
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            if ((bool)type.GetProperty("HasValue", BindingFlags.Public | BindingFlags.Instance).GetValue(value, null)) {
                object wrapped = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance).GetValue(value, null);
                Type dataType = Nullable.GetUnderlyingType(type);
                return SolMarshal.MarshalFromNative(assembly, dataType, wrapped);
            }
            return SolNil.Instance;
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">No matching SolType for the wrapped type.</exception>
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            Type[] genericArgs = type.GetGenericArguments();
            SolType solType = SolMarshal.GetSolType(assembly, genericArgs[0]);
            return new SolType(solType.Type, true);
        }

        #endregion
    }
}