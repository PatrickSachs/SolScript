using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NesterovskyBros.Utils;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Marshal;

namespace SolScript.Interpreter
{
    public static class SolMarshal
    {
        static SolMarshal()
        {
            NativeMarshallers.Add(new NativeNumericMarshaller());
            NativeMarshallers.Add(new NativeCharMarshaller());
            NativeMarshallers.Add(new NativeBoolMarshaller());
            NativeMarshallers.Add(new NativeStringMarshaller());
            NativeMarshallers.Add(new NativeVoidMarshaller());
            NativeMarshallers.Add(new NativeArrayMarshaller());
            NativeMarshallers.Add(new NativeNullableMarshaller());
            NativeMarshallers.Add(new NativeMethodInfoMarshaller());
            SortMarshallers();
        }

        public const int PRIORITY_VERY_HIGH = 1000;
        public const int PRIORITY_HIGH = 500;
        public const int PRIORITY_DEFAULT = 0;
        public const int PRIORITY_LOW = -500;
        public const int PRIORITY_VERY_LOW = -1000;

        private static readonly List<ISolNativeMarshaller> NativeMarshallers = new List<ISolNativeMarshaller>();

        private static readonly Dictionary<SolAssembly, AssemblyCache> s_AssemblyCaches = new Dictionary<SolAssembly, AssemblyCache>();

        private static readonly ClassCreationOptions NativeClassCreationOptions = new ClassCreationOptions.Customizable().SetEnforceCreation(true).SetCallConstructor(false);

        private static void SortMarshallers()
        {
            NativeMarshallers.Sort(delegate(ISolNativeMarshaller m1, ISolNativeMarshaller m2) {
                if (m1.Priority > m2.Priority) {
                    return -1;
                }
                if (m1.Priority < m2.Priority) {
                    return 1;
                }
                return 0;
            });
        }

        public static void RegisterMarshaller(ISolNativeMarshaller marshaller)
        {
            NativeMarshallers.Add(marshaller);
            SortMarshallers();
        }

        public static void RegisterMarshallers(IEnumerable<ISolNativeMarshaller> marshallers)
        {
            NativeMarshallers.AddRange(marshallers);
            SortMarshallers();
        }

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
        public static SolValue MarshalFromNative(SolAssembly assembly, Type type, [CanBeNull] object value)
        {
            if (value == null) {
                return SolNil.Instance;
            }
            if (type == typeof(SolValue) || type.IsSubclassOf(typeof(SolValue))) {
                return (SolValue) value;
            }
            foreach (ISolNativeMarshaller nativeMarshaller in NativeMarshallers) {
                if (nativeMarshaller.DoesHandle(assembly, type)) {
                    return nativeMarshaller.Marshal(assembly, value, type);
                }
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
            throw new SolMarshallingException(type, "No native marshaller has been registered for this type.");
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
            if (type == typeof(SolValue) || type.IsSubclassOf(typeof(SolValue))) {
                return new SolType(SolValue.PrimitiveTypeNameOf(type), true);
            }
            foreach (ISolNativeMarshaller nativeMarshaller in NativeMarshallers) {
                if (nativeMarshaller.DoesHandle(assembly, type)) {
                    return nativeMarshaller.GetSolType(assembly, type);
                }
            }
            if (type.IsClass)
            {
                SolClassDefinition classDef;
                if (assembly.TypeRegistry.TryGetClass(type, out classDef))
                {
                    return new SolType(classDef.Type, true);
                }
            }
            throw new SolMarshallingException(type, "No native marshaller has been registered for this type.");
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