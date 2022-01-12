using System;
using System.Reflection;
using JetBrains.Annotations;
using PSUtility.Enumerables;

namespace PSUtility.Reflection
{
    [PublicAPI]
    public static class AttributeUtility
    {
        /// <inheritdoc cref="MemberInfo.GetCustomAttributes(Type, bool)" />
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="InvalidOperationException">
        ///     This member belongs to a type that is loaded into the reflection-only
        ///     context. See How to: Load Assemblies into the Reflection-Only Context.
        /// </exception>
        [CanBeNull]
        public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit = true) where T : Attribute
        {
            T[] attr = GetCustomAttributes<T>(member, inherit);
            if (attr.Length == 0) {
                return null;
            }
            return attr[0];
        }

        /// <inheritdoc cref="ParameterInfo.GetCustomAttributes(Type, bool)" />
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="ArgumentException">The type must be a type provided by the underlying runtime system.</exception>
        [CanBeNull]
        public static T GetCustomAttribute<T>(this ParameterInfo member, bool inherit = true) where T : Attribute
        {
            T[] attr = GetCustomAttributes<T>(member, inherit);
            if (attr.Length == 0) {
                return null;
            }
            return attr[0];
        }

        /// <inheritdoc cref="MemberInfo.GetCustomAttributes(Type, bool)" />
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="InvalidOperationException">
        ///     This member belongs to a type that is loaded into the reflection-only
        ///     context. See How to: Load Assemblies into the Reflection-Only Context.
        /// </exception>
        public static T[] GetCustomAttributes<T>(this MemberInfo member, bool inherit = true) where T : Attribute
        {
            object[] attr = member.GetCustomAttributes(typeof(T), inherit);
            if (attr.Length == 0) {
                return EmptyArray<T>.Value;
            }
            return (T[]) attr;
        }

        /// <inheritdoc cref="ParameterInfo.GetCustomAttributes(Type, bool)" />
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="ArgumentException">The type must be a type provided by the underlying runtime system.</exception>
        public static T[] GetCustomAttributes<T>(this ParameterInfo member, bool inherit = true) where T : Attribute
        {
            object[] attr = member.GetCustomAttributes(typeof(T), inherit);
            if (attr.Length == 0) {
                return EmptyArray<T>.Value;
            }
            return (T[]) attr;
        }
    }
}