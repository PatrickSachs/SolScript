using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    public static class SolMarshal {
        public static object[] Marshal(SolValue[] values, Type[] types) {
            /*bool lastValuesToArray = false;
            if (values.Length > types.Length) {
                if (types.Length > 0 && !types[types.Length - 1].IsArray) {
                    throw new ArgumentException(
                        "Marshalling requires a type for each value (or the last type needs to be an array)! Got values: " +
                        values.Length +
                        ", Got types: " + types.Length, nameof(values));
                }
                lastValuesToArray = true;
            }
            var outObjects = new object[types.Length];
            for (int i = 0; i < outObjects.Length; i++) {
                Type type = types[i];

                if (lastValuesToArray && i == types.Length - 1) {
                    // We want to support params and are at the last type
                    Type elementType = type.GetElementType();
                    Array paramsArray = Array.CreateInstance(elementType, values.Length - i);
                    for (int v = 0; v < values.Length - i; v++) {
                        object clrArrayObj = Marshal(values[v + i], elementType);
                        paramsArray.SetValue(clrArrayObj, v);
                    }
                    outObjects[i] = paramsArray;
                } else {
                    // Otherwise
                    //SolDebug.WriteLine("marshalling index " + i + " to type " + type);
                    if (values.Length > i) {
                        SolValue value = values[i];
                        //SolDebug.WriteLine("  ... Param was given: " + value);
                        outObjects[i] = type == typeof (SolValue) ? value : value.ConvertTo(type);
                    } else {
                        //SolDebug.WriteLine("  ... Param was NOT given - Trying to infer");
                        if (type == typeof (SolValue) || type.IsSubclassOf(typeof (SolValue))) {
                            //SolDebug.WriteLine("    ... Parameter is or is subclass of solvalue passing nil (flawed)");
                            outObjects[i] = SolNil.Instance;
                        } else {
                            //SolDebug.WriteLine("    ... Parameter is class or struct, creating default value.");
                            outObjects[i] = type.IsClass ? null : Activator.CreateInstance(type);
                        }
                    }
                }
            }*/
            /*foreach (object outObject in outObjects) {
                SolDebug.WriteLine("check --> " + (outObject?.ToString() ?? "<<null>>"));
            }*/
            var marshalled = new object[types.Length];
            MarshalTo(values, types, marshalled, 0);
            return marshalled;
        }

        [CanBeNull]
        public static object Marshal(SolValue value, Type type) {
            return type == typeof (SolValue) ? value : value.ConvertTo(type);
        }

        /// <summary> Marshalls the given values into a pre-created array. This array must
        ///     be of equal size of longer than the amount of given values/types. </summary>
        public static void MarshalTo(SolValue[] values, Type[] types, object[] array, int offset) {
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
                        object clrArrayObj = Marshal(values[v + i], elementType);
                        paramsArray.SetValue(clrArrayObj, v);
                    }
                    iValue = paramsArray;
                } else {
                    // Otherwise
                    //SolDebug.WriteLine("marshalling index " + i + " to type " + type);
                    if (values.Length > i) {
                        SolValue value = values[i];
                        //SolDebug.WriteLine("  ... Param was given: " + value);
                        iValue = type == typeof (SolValue) ? value : value.ConvertTo(type);
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

        public static SolValue MarshalFrom(Type type, [CanBeNull] object value) {
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
                return SolBoolean.ValueOf((bool) value);
            }
            throw new NotImplementedException();
        }


        /// <summary> Gets the most fitting SolType for a given type. </summary>
        public static SolType GetSolType(Type type) {
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                type = type.GetGenericTypeDefinition();
                if (type == typeof (Nullable<>)) {
                    // Recursion will only occur once unless someone nests nullables 
                    //       .. cause tree.
                    SolType solType = GetSolType(genericArgs[0]);
                    return new SolType(solType.Type, true);
                }
            } else {
                if (type == typeof (SolValue) || type == typeof (object)) {
                    return new SolType(SolValue.ANY_TYPE, true);
                }
                if (type == typeof (SolString) || type == typeof (string)) {
                    return SolString.MarshalFromCSharpType;
                }
                if (type == typeof (SolBoolean) || type == typeof (bool)) {
                    return SolBoolean.MarshalFromCSharpType;
                }
                if (type == typeof (SolNumber) || type == typeof (double) || type == typeof (float) ||
                    type == typeof (int)) {
                    return new SolType("number", true);
                }
                if (type == typeof (SolNil) || type == typeof (void)) {
                    return SolNil.MarshalFromCSharpType;
                }
                if (type == typeof (SolTable)) {
                    return new SolType("table", true);
                }
            }
            throw new SolScriptMarshallingException(type);
        }
    }
}