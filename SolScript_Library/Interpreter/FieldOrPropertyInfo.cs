using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace

namespace SolScript.Interpreter
{
    /// <summary> A wrapper to access Field- and PropertyInfo using the same class. </summary>
    public sealed class FieldOrPropertyInfo : MemberInfo
    {
        /// <summary> Creates a new wrapper with an underlying FieldInfo. </summary>
        /// <param name="field"> The field info </param>
        public FieldOrPropertyInfo([NotNull] FieldInfo field)
        {
            m_Member = field;
            m_Field = field;
            IsPrivate = field.IsPrivate;
            IsPublic = field.IsPublic;
            IsStatic = field.IsStatic;
            IsProtected = field.IsFamily;
            IsInternal = field.IsAssembly;
            IsIndexed = false;
        }

        /// <summary> Creates a new wrapper with an underlying PropertyInfo. </summary>
        /// <param name="property"> The property info </param>
        /// <exception cref="InvalidOperationException">Could not obtain a reference to a getter or setter of the property.</exception>
        public FieldOrPropertyInfo([NotNull] PropertyInfo property)
        {
            m_Member = property;
            m_Property = property;
            IsIndexed = property.GetIndexParameters().Length != 0;
            MethodInfo method;
            try {
                method = property.GetGetMethod(false)
                         ?? property.GetGetMethod(true)
                         ?? property.GetSetMethod(false)
                         ?? property.GetSetMethod(true);
            } catch (SecurityException ex) {
                throw new InvalidOperationException("Could not access a getter or setter of proerty \"" + property.Name + "\".", ex);
            }
            if (method == null) {
                throw new InvalidOperationException("The property \"" + property.Name + "\" does not seem to have a getter or setter.");
            }
            IsPrivate = method.IsPrivate;
            IsPublic = method.IsPublic;
            IsStatic = method.IsStatic;
            IsProtected = method.IsFamily;
            IsInternal = method.IsAssembly;
        }

        private readonly FieldInfo m_Field;
        private readonly MemberInfo m_Member;
        private readonly PropertyInfo m_Property;

        /// <summary> Can we read the value of this field/property? </summary>
        public bool CanRead => m_Field != null || m_Property.CanRead;

        /// <summary> Can we write a value to this field/property? </summary>
        public bool CanWrite => m_Field != null ? !m_Field.IsLiteral : m_Property.CanWrite;

        /// <summary> The type of this field/property. </summary>
        public Type DataType => m_Field != null ? m_Field.FieldType : m_Property.PropertyType;

        /// <inheritdoc />
        public override Type DeclaringType => m_Member.DeclaringType;

        /// <summary> Does reading from this field/property require an index. </summary>
        public bool IsIndexed { get; }

        /// <summary>
        ///     Gets a value indicating if this field or property has a special name.
        /// </summary>
        public bool IsSpecialName => m_Field?.IsSpecialName ?? m_Property.IsSpecialName;

        /// <inheritdoc />
        public override MemberTypes MemberType => m_Member.MemberType;

        /// <inheritdoc />
        public override string Name => m_Member.Name;

        /// <inheritdoc />
        public override Type ReflectedType => m_Member.ReflectedType;

        /// <summary>
        ///     Is this field/property internal?
        /// </summary>
        public bool IsInternal { get; private set; }

        /// <summary> Is this this field/property private? </summary>
        public bool IsPrivate { get; private set; }

        public bool IsProtected { get; private set; }

        /// <summary> Is this field/property public? </summary>
        public bool IsPublic { get; private set; }

        /// <summary> Is this field/property static? </summary>
        public bool IsStatic { get; private set; }

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="TypeLoadException">A custom attribute type could not be loaded. </exception>
        /// <exception cref="InvalidOperationException">
        ///     This member belongs to a type that is loaded into the reflection-only
        ///     context. See How to: Load Assemblies into the Reflection-Only Context.
        /// </exception>
        [Pure]
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_Field != null ? m_Field.GetCustomAttributes(attributeType, inherit) : m_Property.GetCustomAttributes(attributeType, inherit);
        }

        /// <summary> Returns a string that represents the current object. </summary>
        /// <returns> A string that represents the current object </returns>
        [Pure]
        public override string ToString()
        {
            return "FieldOrPropertyInfo - " + Name + " (" + DataType.FullName + ")";
        }

        /// <inheritdoc />
        /// <exception cref="TypeLoadException">A custom attribute type could not be loaded. </exception>
        /// <exception cref="InvalidOperationException">
        ///     This member belongs to a type that is loaded into the reflection-only
        ///     context. See How to: Load Assemblies into the Reflection-Only Context.
        /// </exception>
        public override object[] GetCustomAttributes(bool inherit)
        {
            return m_Field != null ? m_Field.GetCustomAttributes(inherit) : m_Property.GetCustomAttributes(inherit);
        }

        /// <summary>
        ///     Checks if this inspector field has at least one attribute of this
        ///     type.
        /// </summary>
        /// <param name="attributeType"> The attribute type </param>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> true if it has one or more attributes, false if not </returns>
        /// <exception cref="TypeLoadException">A custom attribute type could not be loaded. </exception>
        /// <exception cref="InvalidOperationException">
        ///     This member belongs to a type that is loaded into the reflection-only
        ///     context. See How to: Load Assemblies into the Reflection-Only Context.
        /// </exception>
        [Pure]
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return GetCustomAttributes(attributeType, inherit).Length > 0;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null)) {
                return false;
            }
            if (ReferenceEquals(obj, this)) {
                return true;
            }
            FieldOrPropertyInfo fieldOrPropertyInfo = obj as FieldOrPropertyInfo;
            return fieldOrPropertyInfo != null && Equals_Impl(fieldOrPropertyInfo);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (m_Field != null ? m_Field.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Property != null ? m_Property.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion

        /// <summary> Gets all fields/properties of the given type. </summary>
        /// <param name="type">The type to get the fields and properties of.</param>
        /// <param name="flags">The binding flags used to obtain the fields and properties</param>
        /// <param name="includeFields">Should fields be included?</param>
        /// <param name="includeProperties">Should properties be included?</param>
        /// <returns>An enumerable of field and property info wrappers.</returns>
        [Pure]
        public static IEnumerable<FieldOrPropertyInfo> Get([NotNull] Type type, BindingFlags flags, bool includeFields = true,
            bool includeProperties = true)
        {
            if (includeFields) {
                FieldInfo[] fieldInfos = type.GetFields(flags);
                foreach (FieldInfo fieldInfo in fieldInfos) {
                    FieldOrPropertyInfo field = new FieldOrPropertyInfo(fieldInfo);
                    yield return field;
                }
            }
            if (includeProperties) {
                PropertyInfo[] propertyInfos = type.GetProperties(flags);
                foreach (PropertyInfo propertyInfo in propertyInfos) {
                    FieldOrPropertyInfo field = new FieldOrPropertyInfo(propertyInfo);
                    yield return field;
                }
            }
        }

        /// <summary>
        ///     Tries to set the value of this inspector field. Call is ignored if
        ///     the inspector field cannot be written to or is indexed.
        /// </summary>
        /// <param name="target"> The target instance </param>
        /// <param name="value"> The value </param>
        /// <exception cref="FieldAccessException">.The caller does not have permission to access this field/property. </exception>
        /// <exception cref="TargetException">
        ///     The <paramref name="target" /> parameter is null and the field/property is an
        ///     instance field/property.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The <paramref name="value" /> parameter cannot be converted and stored in the
        ///     field/property.
        /// </exception>
        /// <exception cref="TargetParameterCountException">
        ///     This value cannot be set directly. Use <see cref="SetIndexedValue" />
        ///     instead.
        /// </exception>
        /// <exception cref="MethodAccessException">
        ///     There was an illegal attempt to access a private or protected method inside a
        ///     class.
        /// </exception>
        /// <exception cref="TargetInvocationException">
        ///     An error occurred while setting the value. For example, an index value
        ///     specified for an indexed property is out of range. The <see cref="P:System.Exception.InnerException" /> property
        ///     indicates the reason for the error.
        /// </exception>
        public void SetValue([CanBeNull] object target, [CanBeNull] object value)
        {
            if (m_Field != null) {
                m_Field.SetValue(target, value);
            } else {
                m_Property.SetValue(target, value, new object[0]);
            }
        }

        /// <summary>
        ///     Tries to set the indexed value of this inspector field. Call is
        ///     ignored if the inspector field cannot be written to. The value is set
        ///     normally if no indices are required.
        /// </summary>
        /// <param name="target"> The target instance </param>
        /// <param name="value"> The value </param>
        /// <param name="indices"> The indices </param>
        /// <exception cref="TargetException">
        ///     The <paramref name="target" /> parameter is null and the field/property is an
        ///     instance field/property.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The <paramref name="value" /> parameter cannot be converted and stored in the
        ///     field/property.
        /// </exception>
        /// <exception cref="TargetParameterCountException">
        ///     This value cannot be set directly. Use <see cref="SetIndexedValue" />
        ///     instead.
        /// </exception>
        /// <exception cref="MethodAccessException">
        ///     There was an illegal attempt to access a private or protected method inside a
        ///     class.
        /// </exception>
        /// <exception cref="TargetInvocationException">
        ///     An error occurred while setting the value. For example, an index value
        ///     specified for an indexed property is out of range. The <see cref="P:System.Exception.InnerException" /> property
        ///     indicates the reason for the error.
        /// </exception>
        /// <exception cref="FieldAccessException">
        ///     .The caller does not have permission to access this field/property.
        /// </exception>
        public void SetIndexedValue([NotNull] object target, object value, params object[] indices)
        {
            if (m_Field != null || !IsIndexed) {
                SetValue(target, value);
            } else {
                m_Property.SetValue(target, value, indices);
            }
        }

        /// <summary>
        ///     Tries to get the value of this inspector field. Call is ignored if
        ///     this inspector field cannot be read from of is indexed.
        /// </summary>
        /// <param name="target"> The target instance </param>
        /// <returns> The value of this inspector field, null if the call was aborted </returns>
        /// <exception cref="TargetException">
        ///     The <paramref name="target" /> parameter is null and the field/property is an
        ///     instance field/property.
        /// </exception>
        /// <exception cref="TargetParameterCountException">
        ///     This value cannot be set directly. Use <see cref="SetIndexedValue" />
        ///     instead.
        /// </exception>
        /// <exception cref="MethodAccessException">
        ///     There was an illegal attempt to access a private or protected method inside a
        ///     class.
        /// </exception>
        /// <exception cref="TargetInvocationException">
        ///     An error occurred while setting the value. For example, an index value
        ///     specified for an indexed property is out of range. The <see cref="P:System.Exception.InnerException" /> property
        ///     indicates the reason for the error.
        /// </exception>
        /// <exception cref="FieldAccessException">
        ///     .The caller does not have permission to access this field/property.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///     A field is marked literal, but the field does not have one of the accepted
        ///     literal types.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The method is neither declared nor inherited by the class of
        ///     <paramref name="target" />.
        /// </exception>
        [CanBeNull]
        public object GetValue([CanBeNull] object target)
        {
            if (!CanRead || IsIndexed) {
                return null;
            }

            return m_Field != null
                ? m_Field.GetValue(target)
                : m_Property.GetValue(target, new object[0]);
        }

        /// <summary>
        ///     Tries to get the indexed value of this inspector field. Call is
        ///     ignored if this inspector field cannot be read from. The value is retrieved
        ///     normally if no indices are required.
        /// </summary>
        /// <param name="target"> The target instance </param>
        /// <param name="indices"> The indices </param>
        /// <returns> The value of this inspector field, null if the call was aborted </returns>
        /// <exception cref="TargetException">
        ///     The <paramref name="target" /> parameter is null and the field/property is an
        ///     instance field/property.
        /// </exception>
        /// <exception cref="TargetParameterCountException">
        ///     The indexed parameters are incorrect.
        /// </exception>
        /// <exception cref="MethodAccessException">
        ///     There was an illegal attempt to access a private or protected method inside a
        ///     class.
        /// </exception>
        /// <exception cref="TargetInvocationException">
        ///     An error occurred while setting the value. For example, an index value
        ///     specified for an indexed property is out of range. The <see cref="P:System.Exception.InnerException" /> property
        ///     indicates the reason for the error.
        /// </exception>
        /// <exception cref="FieldAccessException">
        ///     .The caller does not have permission to access this field/property.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     The <paramref name="indices" /> array does not contain the type of arguments
        ///     needed.
        /// </exception>
        [CanBeNull]
        public object GetIndexedValue([NotNull] object target, [NotNull] params object[] indices)
        {
            if (!CanRead) {
                return null;
            }

            return m_Field != null || !IsIndexed
                ? GetValue(target)
                : m_Property.GetValue(target, indices);
        }

        /// <summary> Gets a generic attribute from this inspector field. </summary>
        /// <typeparam name="T"> The generic attribute type </typeparam>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> The attribute instance, or null </returns>
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        [CanBeNull, Pure]
        public T GetCustomAttribute<T>(bool inherit = true) where T : Attribute
        {
            T[] attributes = GetCustomAttributes<T>(inherit);
            return attributes.Length == 0
                ? null
                : attributes[0];
        }

        /// <summary> Gets a attribute from this inspector field. </summary>
        /// <param name="attributeType"> The attribute type </param>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> The attribute instance, or null </returns>
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="ArgumentNullException">If <paramref name="attributeType" /> is null.</exception>
        [CanBeNull, Pure]
        public Attribute GetCustomAttribute(Type attributeType, bool inherit = true)
        {
            Attribute[] attributes = GetCustomAttributesCasted(attributeType, inherit);
            return attributes.Length == 0
                ? null
                : attributes[0];
        }

        /// <summary> Gets a generic array of attributes from this inspector field. </summary>
        /// <typeparam name="T"> The generic attribute type </typeparam>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns>
        ///     An array of attribute instances. The array is empty if no attributes
        ///     of this type were found.
        /// </returns>
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        [NotNull, Pure]
        public T[] GetCustomAttributes<T>(bool inherit = true) where T : Attribute
        {
            return (T[]) (m_Field != null
                ? m_Field.GetCustomAttributes(typeof(T), inherit)
                : m_Property.GetCustomAttributes(typeof(T), inherit));
        }

        /// <inheritdoc />
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        /// <exception cref="ArgumentNullException">If <paramref name="attributeType" /> is null.</exception>
        [Pure]
        public Attribute[] GetCustomAttributesCasted(Type attributeType, bool inherit = true)
        {
            return (Attribute[]) (m_Field != null
                ? m_Field.GetCustomAttributes(attributeType, inherit)
                : m_Property.GetCustomAttributes(attributeType, inherit));
        }

        /// <summary>
        ///     Checks if this inspector field has at least one attribute of this
        ///     type.
        /// </summary>
        /// <typeparam name="T"> The generic attribute type </typeparam>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> true if it has one or more attributes, false if not </returns>
        /// <exception cref="TypeLoadException">A custom attribute type cannot be loaded. </exception>
        [Pure]
        public bool IsDefined<T>(bool inherit = true) where T : Attribute
        {
            return GetCustomAttributes<T>(inherit).Length > 0;
        }

        private bool Equals_Impl(FieldOrPropertyInfo other)
        {
            return base.Equals(other) && Equals(m_Field, other.m_Field) && Equals(m_Property, other.m_Property);
        }

        /// <inheritdoc cref="Equals(object)" />
        public bool Equals(FieldOrPropertyInfo obj)
        {
            if (ReferenceEquals(obj, null)) {
                return false;
            }
            if (ReferenceEquals(obj, this)) {
                return true;
            }
            return Equals_Impl(obj);
        }
    }
}