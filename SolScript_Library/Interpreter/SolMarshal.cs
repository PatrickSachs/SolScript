using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public static class SolMarshal {
        private static readonly Dictionary<SolAssembly, AssemblyCache> s_AssemblyCaches = new Dictionary<SolAssembly, AssemblyCache>();

        public static object[] MarshalFromSol(SolValue[] values, Type[] types) {
            var marshalled = new object[types.Length];
            MarshalFromSol(values, types, marshalled, 0);
            return marshalled;
        }

        [CanBeNull]
        public static object MarshalFromSol(SolValue value, Type type) {
            return type == typeof (SolValue) || type.IsSubclassOf(typeof (SolValue)) ? value : value.ConvertTo(type);
        }

        /// <summary> Marshalls the given values into a pre-created array. This array must
        ///     be of equal size of longer than the amount of given values/types. </summary>
        public static void MarshalFromSol(SolValue[] values, Type[] types, object[] array, int offset) {
            if (array.Length - offset < types.Length) {
                throw new ArgumentException("Cannot marshall " + types.Length +
                                            " elements to an array with a length of " +
                                            array.Length + " and offset of " + offset + ".");
            }
            //SolDebug.WriteLine("New go: " + values.Length  + "v/" + types.Length + "t/" + array.Length + "a/" +offset+ "o  \n                      v=" + string.Join(", ", (object[]) values) + "\n                      t=" + string.Join(", ", (object[]) types));
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
                //SolDebug.WriteLine("index " + i + " type" + type);

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
                    //SolDebug.WriteLine("marshalling index " + i + " to type " + type);
                    if (values.Length > i) {
                        SolValue value = values[i];
                        //SolDebug.WriteLine("  ... Param was given: " + value);
                        // unsure about IsSubclassOf just added it without testing
                        iValue = type == typeof (SolValue) || type.IsSubclassOf(typeof (SolValue)) ? value : value.ConvertTo(type);
                    } else {
                        //SolDebug.WriteLine("  ... Param was NOT given - Trying to infer");
                        if (type == typeof (SolValue) || type.IsSubclassOf(typeof (SolValue))) {
                            //SolDebug.WriteLine("    ... Parameter is or is subclass of solvalue passing nil (flawed)");
                            iValue = SolNil.Instance;
                        } else {
                            //SolDebug.WriteLine("    ... Parameter is class or struct, creating default value.");
                            iValue = type.IsClass ? null : Activator.CreateInstance(type);
                        }
                    }
                }
                array[i + offset] = iValue;
            }
            /*SolDebug.WriteLine("=== RESULT ===");
            foreach (object a in array) {
                SolDebug.WriteLine("  - " + a);
            }*/
        }

        public static SolValue MarshalFromCSharp(SolAssembly assembly, Type type, [CanBeNull] object value) {
            if (value == null) {
                return SolNil.Instance;
            }
            if (type == typeof (void)) {
                return SolNil.Instance;
            }
            // todo: provide a way to register them
            if (type == typeof (SolValue) || type.IsSubclassOf(typeof (SolValue))) {
                return (SolValue) value;
            }
            if (type == typeof (double)) {
                return new SolNumber((double) value);
            }
            if (type == typeof (float)) {
                return new SolNumber((float) value);
            }
            if (type == typeof (byte)) {
                return new SolNumber((byte) value);
            }
            if (type == typeof (int)) {
                return new SolNumber((int) value);
            }
            if (type == typeof (uint)) {
                return new SolNumber((uint) value);
            }
            if (type == typeof (long)) {
                return new SolNumber((long) value);
            }
            if (type == typeof (ulong)) {
                return new SolNumber((ulong) value);
            }
            if (type == typeof (short)) {
                return new SolNumber((short) value);
            }
            if (type == typeof (ushort)) {
                return new SolNumber((ushort) value);
            }
            if (type == typeof (string)) {
                return new SolString(value as string ?? string.Empty);
            }
            if (type == typeof (bool)) {
                return SolBool.ValueOf((bool) value);
            }
            if (type.IsClass) {
                AssemblyCache cache = GetAssemblyCache(assembly);
                SolClass solClass = cache.GetReference(value);
                if (solClass == null) {
                    SolClassDefinition classDef;
                    if (!assembly.TypeRegistry.TryGetClass(type, out classDef)) {
                        throw new InvalidOperationException("Cannot marshal type " + type + " to SolScript: The type is not marked as sol class!");
                    }
                    SolClass.Initializer init = assembly.TypeRegistry.CreateInstance(classDef);
                    solClass = init.CreateWithoutInitialization();
                    solClass.InheritanceChain.NativeObject = value;
                    solClass.IsInitialized = true;
                    cache.StoreReference(value, solClass);
                }
                return solClass;
            }
            throw new NotImplementedException();
        }


        /// <summary> Gets the most fitting SolType for a given type. </summary>
        public static SolType GetSolType(SolAssembly assembly, Type type) {
            if (type.IsGenericType) {
                var genericArgs = type.GetGenericArguments();
                type = type.GetGenericTypeDefinition();
                if (type == typeof (Nullable<>)) {
                    // Recursion will only occur once unless someone nests nullables 
                    //       .. cause tree.
                    SolType solType = GetSolType(assembly, genericArgs[0]);
                    return new SolType(solType.Type, true);
                }
            } else {
                if (type.IsArray) return new SolType("table");
                if (type == typeof (SolValue) || type == typeof (object)) {
                    return new SolType(SolValue.ANY_TYPE, true);
                }
                if (type == typeof (SolClass)) {
                    // todo: handle SolClass! (maybe "class" type constraint?)
                    return new SolType(SolValue.ANY_TYPE, true);
                }
                if (type == typeof (SolString) || type == typeof (string)) {
                    return new SolType("string", true);
                }
                if (type == typeof (SolBool) || type == typeof (bool)) {
                    return new SolType("bool", false);
                }
                if (type == typeof (SolNumber) || type == typeof (double) || type == typeof (float) ||
                    type == typeof (int)) {
                    return new SolType("number", false);
                }
                if (type == typeof (SolNil) || type == typeof (void)) {
                    return new SolType("nil", true);
                }
                if (type == typeof (SolTable)) {
                    return new SolType("table", true);
                }
                if (type == typeof (SolFunction)) {
                    return new SolType("function", true);
                }
                if (type.IsClass) {
                    SolClassDefinition classDef;
                    if (assembly.TypeRegistry.TryGetClass(type, out classDef)) {
                        return new SolType(classDef.Type, true);
                    }
                }
            }
            throw new SolScriptMarshallingException(type);
        }

        private static AssemblyCache GetAssemblyCache(SolAssembly assembly) {
            AssemblyCache cache;
            if (!s_AssemblyCaches.TryGetValue(assembly, out cache)) {
                cache = new AssemblyCache(assembly);
                s_AssemblyCaches[assembly] = cache;
            }
            return cache;
        }

        #region Nested type: AssemblyCache

        /// <summary> The assembly cache is used to weakly store native objects and their
        ///     respective SolClass representations. </summary>
        private class AssemblyCache {
            public AssemblyCache([NotNull] SolAssembly assembly) {
                Assembly = assembly;
                m_NativeToSol = new ConditionalWeakTable<object, SolClass>();
            }

            [NotNull] public readonly SolAssembly Assembly;

            private readonly ConditionalWeakTable<object, SolClass> m_NativeToSol;

            public void StoreReference([NotNull] object value, [NotNull] SolClass solClass) {
                m_NativeToSol.Add(value, solClass);
            }

            [CanBeNull]
            public SolClass GetReference([NotNull] object value) {
                SolClass solClass;
                return m_NativeToSol.TryGetValue(value, out solClass) ? solClass : null;
            }
        }

        #endregion
    }
}