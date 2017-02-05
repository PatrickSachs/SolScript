using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NesterovskyBros.Utils;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    public static class SolMarshal
    {
        private static readonly Dictionary<SolAssembly, AssemblyCache> s_AssemblyCaches = new Dictionary<SolAssembly, AssemblyCache>();

        private static readonly ClassCreationOptions NativeClassCreationOptions = new ClassCreationOptions.Customizable().SetEnforceCreation(true).SetCallConstructor(false);

        public static object[] MarshalFromSol(SolAssembly assembly, SolValue[] values, Type[] types)
        {
            var marshalled = new object[types.Length];
            MarshalFromSol(assembly, values, types, marshalled, 0);
            return marshalled;
        }

        [CanBeNull]
        public static object MarshalFromSol(SolValue value, Type type)
        {
            return type == typeof(SolValue) || type.IsSubclassOf(typeof(SolValue)) ? value : value.ConvertTo(type);
        }

        /*/// <summary>
        ///     Marshalls the given values into a pre-created array. This array must
        ///     be of equal size of longer than the amount of given values/types.
        /// </summary>
        /// <exception cref="SolMarshallingException">An error occured during marshalling.</exception>
        public static void MarshalFromSol(SolAssembly assembly, SolValue[] values, Type[] types, object[] array, int offset)
        {
            if (array.Length - offset < types.Length) {
                throw new ArgumentException("Cannot marshall " + types.Length +
                                            " elements to an array with a length of " +
                                            array.Length + " and offset of " + offset + ".");
            }
            // If the last type is an array we will put all excess values into this array.
            bool lastValuesToArray = types.Length > 0 && types[types.Length - 1].IsArray;
            if (!lastValuesToArray && values.Length > types.Length) {
                throw new ArgumentException(
                    "Marshalling requires a type for each value (or the last type needs to be an array)! Got values: " +
                    values.Length +
                    ", Got types: " + types.Length, nameof(values));
            }
            for (int i = 0; i < types.Length; i++) {
                Type type = types[i];
                object iValue;
                if (lastValuesToArray && i == types.Length - 1) {
                    // We want to support params and are at the last type
                    Type elementType = type.GetElementType();
                    Array paramsArray = Array.CreateInstance(elementType, values.Length - i);
                    for (int v = 0; v < values.Length - i; v++) {
                        object clrArrayObj = MarshalFromSol(values[v + i], elementType);
                        paramsArray.SetValue(clrArrayObj, v);
                    }
                    iValue = paramsArray;
                } else {
                    // Otherwise
                    if (values.Length > i) {
                        SolValue value = values[i];
                        // unsure about IsSubclassOf just added it without testing
                        iValue = type == typeof(SolValue) || type.IsSubclassOf(typeof(SolValue)) ? value : value.ConvertTo(type);
                    } else {
                        if (type == typeof(SolValue) || type.IsSubclassOf(typeof(SolValue))) {
                            iValue = SolNil.Instance;
                        } else {
                            try {
                                // todo: use cache (?)
                                iValue = type.IsClass ? null : Activator.CreateInstance(type);
                            } catch (TargetInvocationException ex) {
                                throw new SolMarshallingException($"A native error occured while creating a class instance of native type \"{type}\".", ex);
                            }
                        }
                    }
                }
                array[i + offset] = iValue;
            }
        }*/

        /// <exception cref="SolMarshallingException">Failed to marshal a value.</exception>
        public static void MarshalFromSol(SolAssembly assembly, SolValue[] values, Type[] types, object[] array, int offset)
        {
            MarshalFromSol(assembly, 0, values.Length, values, types, array, offset);
        }

        /// <summary>
        ///     Marshals multiple values from their SolScript representation to their native counerparts.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="valueStart">The start index in the <paramref name="source" /> array.</param>
        /// <param name="valueCount">How many values should be marshalled from the <paramref name="source" /> array?</param>
        /// <param name="source">The source array to get the value to marshal from.</param>
        /// <param name="types">The native types the values should be marshalled to.</param>
        /// <param name="target">The target array to put the marshalled values in. Must be initialized and of sufficient size.</param>
        /// <param name="offset">The offset in the <paramref name="target" /> array when inserting the marshalled values.</param>
        /// <exception cref="SolMarshallingException">Failed to marshal a value.</exception>
        /// <exception cref="ArgumentException">Array length mismatches.</exception>
        public static void MarshalFromSol(SolAssembly assembly, int valueStart, int valueCount, SolValue[] source, Type[] types, object[] target, int offset)
        {
            if (valueCount != types.Length || valueStart + valueCount > source.Length || valueCount < 0) {
                throw new ArgumentException($"Marshalling requires a type for each value - Got {source.Length}(Overridden to {valueCount}, starting at {valueStart}) values and {types.Length} types.",
                    nameof(source));
            }
            if (target.Length - offset < valueCount) {
                throw new ArgumentException($"Cannot marshall {types.Length} elements to an array with a length of {target.Length} with an offset of {offset}.");
            }
            for (int i = 0; i < types.Length; i++) {
                Type type = types[i];
                SolValue value = source[valueStart + i];
                object nativeValue;
                if (type.IsInstanceOfType(value)) {
                    // The value can be directly assigned to the desired type, no conversion needed.
                    nativeValue = value;
                } else {
                    // We need type conversion. Hey ho, let's go.
                    if (value.IsClass) {
                        SolClass valueClass = (SolClass) value;
                        // Let's see if at any point in the hierarchy the class could be assigned to this native type.
                        SolClass.Inheritance inheritance = valueClass.FindInheritance(type);
                        if (inheritance != null) {
                            nativeValue = inheritance.NativeObject;
                            if (nativeValue == null) {
                                throw new SolMarshallingException($"The native object required to correctly marshal a class of type \"{value.Type}\" to \"{type.Name}\" has not been iniitalized.");
                            }
                            // Store the reference in the assembly cache for lookup if the class would be marshalled back.
                            GetAssemblyCache(assembly).StoreReference(nativeValue, valueClass);
                        } else {
                            nativeValue = value.ConvertTo(type);
                        }
                    } else {
                        nativeValue = value.ConvertTo(type);
                    }
                }
                target[i + offset] = nativeValue;
            }
        }

        /// <summary>
        ///     Marshals the given native object to a <see cref="SolValue" /> useable in SolScript.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="type">
        ///     The type of the native object. Typically a <see cref="object.GetType()" /> call should be good
        ///     enough for this argument unless you expect <paramref name="value" /> to be null.
        /// </param>
        /// <param name="value">The value to marshal.</param>
        /// <returns>The marshalled <see cref="SolValue" />.</returns>
        /// <exception cref="SolMarshallingException">Failed to marshal the given value.</exception>
        public static SolValue MarshalFromCSharp(SolAssembly assembly, Type type, [CanBeNull] object value)
        {
            if (value == null) {
                return SolNil.Instance;
            }
            if (type == typeof(void)) {
                return SolNil.Instance;
            }
            // todo: provide a way to register them
            if (type == typeof(SolValue) || type.IsSubclassOf(typeof(SolValue))) {
                return (SolValue) value;
            }
            if (type == typeof(double)) {
                return new SolNumber((double) value);
            }
            if (type == typeof(float)) {
                return new SolNumber((float) value);
            }
            if (type == typeof(byte)) {
                return new SolNumber((byte) value);
            }
            if (type == typeof(int)) {
                return new SolNumber((int) value);
            }
            if (type == typeof(uint)) {
                return new SolNumber((uint) value);
            }
            if (type == typeof(long)) {
                return new SolNumber((long) value);
            }
            if (type == typeof(ulong)) {
                return new SolNumber((ulong) value);
            }
            if (type == typeof(short)) {
                return new SolNumber((short) value);
            }
            if (type == typeof(ushort)) {
                return new SolNumber((ushort) value);
            }
            if (type == typeof(string)) {
                return new SolString(value as string ?? string.Empty);
            }
            if (type == typeof(bool)) {
                return SolBool.ValueOf((bool) value);
            }
            if (type.IsArray) {
                Array array = (Array) value;
                SolTable table = new SolTable();
                for (int i = 0; i < array.Length; i++) {
                    object iValue = array.GetValue(i);
                    table.Append(MarshalFromCSharp(assembly, iValue.GetType(), iValue));
                }
                return table;
            }
            if (type.IsClass) {
                AssemblyCache cache = GetAssemblyCache(assembly);
                SolClass solClass = cache.GetReference(value);
                if (solClass == null) {
                    SolClassDefinition classDef;
                    if (!assembly.TypeRegistry.TryGetClass(type, out classDef)) {
                        throw new SolMarshallingException($"Cannot marshal native type \"{type}\" to SolScript: This type does not have a SolClass representing it.");
                    }
                    // todo: inestigate if this order will cause problems (annotations specifically)
                    try {
                        solClass = assembly.TypeRegistry.CreateInstance(classDef, NativeClassCreationOptions);
                    } catch (SolTypeRegistryException ex) {
                        throw new SolMarshallingException(
                            $"Cannot marshal native type \"{type}\" to SolScript: A error occured while creating its representing class instance of type \"" + classDef.Type + "\".", ex);
                    }
                    solClass.InheritanceChain.NativeObject = value;
                    cache.StoreReference(value, solClass);
                }
                return solClass;
            }
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Finds a matching SolType for the given native type.
        /// </summary>
        /// <param name="assembly">The assembly to use.</param>
        /// <param name="type">The native type.</param>
        /// <returns>The SolType.</returns>
        /// <exception cref="SolMarshallingException">No matching SolType for this native type.</exception>
        public static SolType GetSolType(SolAssembly assembly, Type type)
        {
            if (type.IsGenericType) {
                Type[] genericArgs = type.GetGenericArguments();
                type = type.GetGenericTypeDefinition();
                if (type == typeof(Nullable<>)) {
                    // Recursion will only occur once unless someone nests nullables 
                    //       .. cause tree.
                    SolType solType = GetSolType(assembly, genericArgs[0]);
                    return new SolType(solType.Type, true);
                }
            } else {
                if (type.IsArray) {
                    return new SolType("table", false);
                }
                if (type == typeof(SolValue) || type == typeof(object)) {
                    return new SolType(SolValue.ANY_TYPE, true);
                }
                if (type == typeof(SolClass)) {
                    // todo: handle SolClass! (maybe "class" type constraint?)
                    return new SolType(SolValue.ANY_TYPE, true);
                }
                if (type == typeof(SolString) || type == typeof(string)) {
                    return new SolType("string", true);
                }
                if (type == typeof(SolBool) || type == typeof(bool)) {
                    return new SolType("bool", false);
                }
                if (type == typeof(SolNumber) || type == typeof(double) || type == typeof(float) ||
                    type == typeof(int)) {
                    return new SolType("number", false);
                }
                if (type == typeof(SolNil) || type == typeof(void)) {
                    return new SolType("nil", true);
                }
                if (type == typeof(SolTable)) {
                    return new SolType("table", true);
                }
                if (type == typeof(SolFunction)) {
                    return new SolType("function", true);
                }
                if (type.IsClass) {
                    SolClassDefinition classDef;
                    if (assembly.TypeRegistry.TryGetClass(type, out classDef)) {
                        return new SolType(classDef.Type, true);
                    }
                }
            }
            throw new SolMarshallingException(type);
        }

        private static AssemblyCache GetAssemblyCache(SolAssembly assembly)
        {
            AssemblyCache cache;
            if (!s_AssemblyCaches.TryGetValue(assembly, out cache)) {
                cache = new AssemblyCache(assembly);
                s_AssemblyCaches[assembly] = cache;
            }
            return cache;
        }

        #region Nested type: AssemblyCache

        /// <summary>
        ///     The assembly cache is used to weakly store native objects and their
        ///     respective SolClass representations.
        /// </summary>
        private class AssemblyCache
        {
            public AssemblyCache([NotNull] SolAssembly assembly)
            {
                Assembly = assembly;
                m_NativeToSol = new WeakTable<object, SolClass>();
            }

            [NotNull] public readonly SolAssembly Assembly;

            private readonly WeakTable<object, SolClass> m_NativeToSol;

            public bool StoreReference([NotNull] object value, [NotNull] SolClass solClass)
            {
                return m_NativeToSol.TryAdd(value, solClass);
            }

            [CanBeNull]
            public SolClass GetReference([NotNull] object value)
            {
                SolClass solClass;
                return m_NativeToSol.TryGetValue(value, out solClass) ? solClass : null;
            }
        }

        #endregion
    }
}