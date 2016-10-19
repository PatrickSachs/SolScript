using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace

namespace SevenBiT.Inspector {
    /// <summary> A wrapper to access Field- and PropertyInfo using the same class. </summary>
    public sealed class InspectorField {
        /// <summary> Creates a new wrapper with an underlying FieldInfo. </summary>
        /// <param name="field"> The field info </param>
        public InspectorField([NotNull] FieldInfo field) {
            this.field = field;
            CanWrite = !field.IsLiteral;
            Name = field.Name;
            IsPrivate = field.IsPrivate;
            IsPublic = field.IsPublic;
            IsStatic = field.IsStatic;
            IsProtected = field.IsFamily;
            IsInternal = field.IsAssembly;
            DataType = field.FieldType;
            CanRead = true;
            IsIndexed = false;
            DeclaringType = field.DeclaringType;
        }

        /// <summary> Creates a new wrapper with an underlying PropertyInfo. </summary>
        /// <param name="property"> The property info </param>
        public InspectorField([NotNull] PropertyInfo property) {
            this.property = property;
            CanRead = property.CanRead;
            CanWrite = property.CanWrite;
            IsIndexed = property.GetIndexParameters().Length != 0;
            Name = property.Name;

            DataType = property.PropertyType;
            // TODO: Check all methods
            MethodInfo method = property.GetGetMethod(false)
                                ?? property.GetGetMethod(true)
                                ?? property.GetSetMethod(false)
                                ?? property.GetSetMethod(true);
            if (method != null) {
                IsPrivate = method.IsPrivate;
                IsPublic = method.IsPublic;
                IsStatic = method.IsStatic;
                IsProtected = method.IsFamily;
                IsInternal = method.IsAssembly;
            }
            DeclaringType = property.DeclaringType;
        }

        private readonly FieldInfo field;
        private readonly PropertyInfo property;

        public Type DeclaringType { get; private set; }

        /// <summary> The name of this inspector field. </summary>
        public string Name { get; }

        /// <summary> Can we write a value to this inspector field? </summary>
        public bool CanWrite { get; }

        /// <summary> Can we read the value of this inspector field? </summary>
        public bool CanRead { get; }

        /// <summary> Is this this inspector field private? </summary>
        public bool IsPrivate { get; private set; }

        /// <summary> Is this inspector field public? </summary>
        public bool IsPublic { get; private set; }

        public bool IsProtected { get; private set; }
        public bool IsInternal { get; private set; }

        /// <summary> Is this inspector field static? </summary>
        public bool IsStatic { get; private set; }

        /// <summary> The type of this inspector field. </summary>
        public Type DataType { get; }

        /// <summary> Does reading from this inspector field require an index. </summary>
        public bool IsIndexed { get; }

        /// <summary> Gets all inspector fields or the given type. </summary>
        [Pure]
        public static IEnumerable<InspectorField> GetInspectorFields([NotNull] Type type, bool includeFields = true,
            bool includeProperties = true) {
            while (type != null && type != typeof (object)) {
                if (includeFields) {
                    var fieldInfos =
                        type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                       BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (FieldInfo fieldInfo in fieldInfos) {
                        InspectorField field = new InspectorField(fieldInfo);
                        yield return field;
                    }
                }
                if (includeProperties) {
                    var propertyInfos =
                        type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                                           BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (PropertyInfo propertyInfo in propertyInfos) {
                        InspectorField field = new InspectorField(propertyInfo);
                        yield return field;
                    }
                }
                type = type.BaseType;
            }
        }

        /// <summary> Tries to set the value of this inspector field. Call is ignored if
        ///     the inspector field cannot be written to or is indexed. </summary>
        /// <param name="target"> The target instance </param>
        /// <param name="value"> The value </param>
        public void SetValue([CanBeNull] object target, [CanBeNull] object value) {
            if (!CanWrite || IsIndexed)
                return;

            if (field != null) {
                field.SetValue(target, value);
            } else {
                property.SetValue(target, value, new object[0]);
            }
        }

        /// <summary> Tries to set the indexed value of this inspector field. Call is
        ///     ignored if the inspector field cannot be written to. The value is set
        ///     normally if no indices are required. </summary>
        /// <param name="target"> The target instance </param>
        /// <param name="value"> The value </param>
        /// <param name="indices"> The indices </param>
        public void SetIndexedValue([NotNull] object target, object value, params object[] indices) {
            if (!CanWrite)
                return;

            if (field != null || !IsIndexed) {
                SetValue(target, value);
            } else {
                property.SetValue(target, value, indices);
            }
        }

        /// <summary> Tries to get the value of this inspector field. Call is ignored if
        ///     this inspector field cannot be read from of is indexed. </summary>
        /// <param name="target"> The target instance </param>
        /// <returns> The value of this inspector field, null if the call was aborted </returns>
        [CanBeNull]
        public object GetValue([CanBeNull] object target) {
            if (!CanRead || IsIndexed)
                return null;

            return field != null
                ? field.GetValue(target)
                : property.GetValue(target, new object[0]);
        }

        /// <summary> Tries to get the indexed value of this inspector field. Call is
        ///     ignored if this inspector field cannot be read from. The value is retrieved
        ///     normally if no indices are required. </summary>
        /// <param name="target"> The target instance </param>
        /// <param name="indices"> The indices </param>
        /// <returns> The value of this inspector field, null if the call was aborted </returns>
        [CanBeNull]
        public object GetIndexedValue([NotNull] object target, [NotNull] params object[] indices) {
            if (!CanRead)
                return null;

            return field != null || !IsIndexed
                ? GetValue(target)
                : property.GetValue(target, indices);
        }

        /// <summary> Gets a generic attribute from this inspector field. </summary>
        /// <typeparam name="T"> The generic attribute type </typeparam>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> The attribute instance, or null </returns>
        [CanBeNull, Pure]
        public T GetAttribute<T>(bool inherit) where T : Attribute {
            var attributes = GetAttributes<T>(inherit);
            return attributes.Length == 0
                ? null
                : attributes[0];
        }

        /// <summary> Gets a attribute from this inspector field. </summary>
        /// <param name="attributeType"> The attribute type </param>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> The attribute instance, or null </returns>
        [CanBeNull, Pure]
        public Attribute GetAttribute(Type attributeType, bool inherit) {
            var attributes = GetAttributes(attributeType, inherit);
            return attributes.Length == 0
                ? null
                : attributes[0];
        }

        /// <summary> Gets a generic array of attributes from this inspector field. </summary>
        /// <typeparam name="T"> The generic attribute type </typeparam>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> An array of attribute instances. The array is empty if no attributes
        ///     of this type were found. </returns>
        [NotNull, Pure]
        public T[] GetAttributes<T>(bool inherit) where T : Attribute {
            return (T[]) (field != null
                ? field.GetCustomAttributes(typeof (T), inherit)
                : property.GetCustomAttributes(typeof (T), inherit));
        }

        /// <summary> Gets a array of attributes from this inspector field. </summary>
        /// <param name="attributeType"> The attribute type </param>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> An array of attribute instances. The array is empty if no attributes
        ///     of this type were found. </returns>
        [NotNull, Pure]
        public Attribute[] GetAttributes(Type attributeType, bool inherit) {
            return (Attribute[]) (field != null
                ? field.GetCustomAttributes(attributeType, inherit)
                : property.GetCustomAttributes(attributeType, inherit));
        }

        /// <summary> Checks if this inspector field has at least one attribute of this
        ///     type. </summary>
        /// <typeparam name="T"> The generic attribute type </typeparam>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> true if it has one or more attributes, false if not </returns>
        [Pure]
        public bool HasAttribute<T>(bool inherit) where T : Attribute {
            return GetAttributes<T>(inherit).Length > 0;
        }

        /// <summary> Checks if this inspector field has at least one attribute of this
        ///     type. </summary>
        /// <param name="attributeType"> The attribute type </param>
        /// <param name="inherit"> Inherit from base types? </param>
        /// <returns> true if it has one or more attributes, false if not </returns
        [Pure]
        public bool HasAttribute(Type attributeType, bool inherit) {
            return GetAttributes(attributeType, inherit).Length > 0;
        }

        /// <summary> Returns a string that represents the current object. </summary>
        /// <returns> A string that represents the current object </returns>
        [Pure]
        public override string ToString() {
            return "InspectorField - " + Name + " (" + DataType.FullName + ")";
        }
    }
}