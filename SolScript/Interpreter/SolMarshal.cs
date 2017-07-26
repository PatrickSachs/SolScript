// ---------------------------------------------------------------------
// SolScript - A simple but powerful scripting language.
// Official repository: https://bitbucket.org/PatrickSachs/solscript/
// ---------------------------------------------------------------------
// Copyright 2017 Patrick Sachs
// Permission is hereby granted, free of charge, to any person obtaining 
// a copy of this software and associated documentation files (the 
// "Software"), to deal in the Software without restriction, including 
// without limitation the rights to use, copy, modify, merge, publish, 
// distribute, sublicense, and/or sell copies of the Software, and to 
// permit persons to whom the Software is furnished to do so, subject to 
// the following conditions:
// 
// The above copyright notice and this permission notice shall be 
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN 
// ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.
// ---------------------------------------------------------------------
// ReSharper disable ArgumentsStyleStringLiteral

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using PSUtility.Strings;
using SolScript.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Marshal;
using SolScript.Properties;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This static class is used to marshal; to "translate" values between SolScript and .NET. References are typically
    ///     upheld when marshalling back and forth.
    /// </summary>
    public static class SolMarshal
    {
        #region NativeClassRepresentation enum

        /// <summary>
        ///     How should the type of a native class be represented? (Used by <see cref="SolMarshal.GetNativeSolType" /> method)
        /// </summary>
        public enum NativeClassRepresentation
        {
            /// <summary>
            ///     Not at all - Native types should be returned as SolClass type.
            /// </summary>
            SolClass,

            /// <summary>
            ///     The described type should be returned. (Return value can be null for script defined classes!)
            /// </summary>
            DescribedType,

            /// <summary>
            ///     The descriptor type should be returned. (Return value can be null for script defined classes!)
            /// </summary>
            DescriptorType
        }

        #endregion

        static SolMarshal()
        {
            s_NativeMarshallers.Add(new NativeNumericMarshaller());
            s_NativeMarshallers.Add(new NativeCharMarshaller());
            s_NativeMarshallers.Add(new NativeBoolMarshaller());
            s_NativeMarshallers.Add(new NativeStringMarshaller());
            s_NativeMarshallers.Add(new NativeVoidMarshaller());
            s_NativeMarshallers.Add(new NativeNullableMarshaller());
            s_NativeMarshallers.Add(new NativeMethodInfoMarshaller());
            s_NativeMarshallers.Add(new NativeStringBuilderMarshaller());
            s_NativeMarshallers.Add(new NativeDictionaryMarshaller());
            s_NativeMarshallers.Add(new NativeEnumerableMarshaller());
            s_NativeMarshallers.Add(new NativeDelegateMarshaller());
            s_NativeMarshallers.Add(new NativeEnumMarshaller());
            s_NativeMarshallers.Add(new NativeClassMarshaller());
            s_NativeMarshallers.Add(new NativeObjectMarshaller());
            s_NativeMarshallers.Sort(Comparer.Instance);
        }

        public const int PRIORITY_VERY_HIGH = 1000;
        public const int PRIORITY_HIGH = 500;
        public const int PRIORITY_DEFAULT = 0;
        public const int PRIORITY_LOW = -500;
        public const int PRIORITY_VERY_LOW = -1000;

        /// <summary>
        ///     All registered native marshallers, in order.
        /// </summary>
        private static readonly List<ISolNativeMarshaller> s_NativeMarshallers = new List<ISolNativeMarshaller>();

        /// <summary>
        ///     Registers a single marshaller and then sorts the marshaller list.
        /// </summary>
        /// <param name="marshaller">The marshaller.</param>
        public static void RegisterMarshaller(ISolNativeMarshaller marshaller)
        {
            s_NativeMarshallers.Add(marshaller);
            s_NativeMarshallers.Sort(Comparer.Instance);
        }

        /// <summary>
        ///     Registers multiple marshallers and then sorts the marshaller list.
        /// </summary>
        /// <param name="marshallers">The marshaller.</param>
        public static void RegisterMarshallers(IEnumerable<ISolNativeMarshaller> marshallers)
        {
            s_NativeMarshallers.AddRange(marshallers);
            s_NativeMarshallers.Sort(Comparer.Instance);
        }

        /// <inheritdoc cref="MarshalFromSol(int,int,SolValue[],Type[],object[],int)" />
        /// <exception cref="SolMarshallingException">Failed to marshal a value.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Array length mismatches.</exception>
        public static object[] MarshalFromSol(SolValue[] values, Type[] types)
        {
            var marshalled = new object[types.Length];
            MarshalFromSol(values, types, marshalled, 0);
            return marshalled;
        }

        /// <inheritdoc cref="MarshalFromSol(SolValue, Type)" />
        /// <exception cref="SolMarshallingException">Failed to marshal the value.</exception>
        public static T MarshalFromSol<T>(SolValue value)
        {
            return (T) MarshalFromSol(value, typeof(T));
        }

        /// <inheritdoc cref="MarshalFromSol(int,int,SolValue[],Type[],object[],int)" />
        /// <exception cref="SolMarshallingException">Failed to marshal the value.</exception>
        public static object MarshalFromSol(SolValue value, Type type)
        {
            return type == typeof(SolValue) || type.IsSubclassOf(typeof(SolValue)) ? value : value.ConvertTo(type);
        }

        /// <inheritdoc cref="MarshalFromSol(int,int,SolValue[],Type[],object[],int)" />
        /// <exception cref="SolMarshallingException">Failed to marshal a value.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Array length mismatches.</exception>
        public static void MarshalFromSol(SolValue[] values, Type[] types, object[] array, int offset = 0)
        {
            MarshalFromSol(0, values.Length, values, types, array, offset);
        }

        /// <summary>
        ///     Marshals multiple values from their SolScript representation to their native counerparts.
        /// </summary>
        /// <param name="valueStart">The start index in the <paramref name="values" /> array.</param>
        /// <param name="valueCount">How many values should be marshalled from the <paramref name="values" /> array?</param>
        /// <param name="values">The values array to get the value to marshal from.</param>
        /// <param name="types">The native types the values should be marshalled to.</param>
        /// <param name="array">The target array to put the marshalled values in. Must be initialized and of sufficient size.</param>
        /// <param name="offset">The offset in <paramref name="array" /> when inserting the marshalled values.</param>
        /// <exception cref="SolMarshallingException">Failed to marshal a value.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Array length mismatches.</exception>
        public static void MarshalFromSol(int valueStart, int valueCount, [ItemNotNull] SolValue[] values, [ItemNotNull] Type[] types, object[] array, int offset)
        {
            if (valueCount != types.Length || valueStart + valueCount > values.Length || valueCount < 0 || array.Length - offset < valueCount) {
                throw new ArgumentOutOfRangeException(Resources.Err_MarshallingArrayMismatch.FormatWith(
                    values.Length, types.Length, array.Length, valueStart, valueCount, offset), nameof(values));
            }
            for (int i = 0; i < types.Length; i++) {
                Type type = types[i];
                SolValue value = values[valueStart + i];
                object nativeValue;
                if (type == typeof(SolValue) || type.IsSubclassOf(typeof(SolValue))) {
                    if (!type.IsInstanceOfType(value)) {
                        if (value is SolNil) {
                            nativeValue = null;
                        } else {
                            throw new SolMarshallingException(value.Type, type);
                        }
                    } else {
                        nativeValue = value;
                    }
                } else {
                    nativeValue = value.ConvertTo(type);
                }
                array[i + offset] = nativeValue;
            }
        }

        /// <summary>
        ///     Marshals the given native values to their SolValue representations.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="values">The values to marshal.</param>
        /// <returns>The marshalled <see cref="SolValue" />s.</returns>
        /// <exception cref="SolMarshallingException">Failed to marshal the given value.</exception>
        /// <remarks>
        ///     Keep in mind that the types of the values are inferred using <see cref="object.GetType()" /> which only
        ///     returns the most derived type.
        /// </remarks>
        public static SolValue[] MarshalFromNative(SolAssembly assembly, [ItemCanBeNull] params object[] values)
        {
            var array = new SolValue[values.Length];
            for (int i = 0; i < array.Length; i++) {
                object value = values[i];
                array[i] = MarshalFromNative(assembly, value?.GetType() ?? typeof(void), value);
            }
            return array;
        }

        /// <summary>
        ///     Gets a SolScript native type representing a certain type name.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="type">The type name.</param>
        /// <param name="classRepresentation">How should native classes be represented?</param>
        /// <returns>The type.</returns>
        /// <remarks>
        ///     This method will return types in the form of <see cref="SolBool" /> and <see cref="SolTable" />. Pay attention
        ///     to the <paramref name="classRepresentation" /> parameter: By default all classes are returned as
        ///     <see cref="SolClass" /> type.
        /// </remarks>
        /// <seealso cref="NativeClassRepresentation" />
        /// <exception cref="SolMarshallingException">Cannot find a native type.</exception>
        public static Type GetNativeSolType(SolAssembly assembly, string type, NativeClassRepresentation classRepresentation = NativeClassRepresentation.SolClass)
        {
            switch (type) {
                case SolValue.ANY_TYPE:
                    return typeof(SolValue);
                case SolValue.CLASS_TYPE:
                    return typeof(SolClass);
                case SolNil.TYPE:
                    return typeof(SolNil);
                case SolNumber.TYPE:
                    return typeof(SolNumber);
                case SolBool.TYPE:
                    return typeof(SolBool);
                case SolString.TYPE:
                    return typeof(SolString);
                case SolFunction.TYPE:
                    return typeof(SolFunction);
                case SolTable.TYPE:
                    return typeof(SolTable);
                default: {
                    SolClassDefinition definition;
                    if (assembly.TryGetClass(type, out definition)) {
                        switch (classRepresentation) {
                            case NativeClassRepresentation.SolClass:
                                return typeof(SolClass);
                            case NativeClassRepresentation.DescribedType:
                                if (definition.DescribedType != null) {
                                    return definition.DescribedType;
                                }
                                break;
                            case NativeClassRepresentation.DescriptorType:
                                if (definition.DescriptorType != null) {
                                    return definition.DescriptorType;
                                }
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(classRepresentation), classRepresentation, null);
                        }
                        return typeof(object);
                    }
                    break;
                }
            }
            throw new SolMarshallingException("Cannot find a native type for SolType \"" + type + "\".");
        }

        /*/// <summary>
        ///     Gets a commonyl used native type describing a certain SolScript type. Be careful with the results of this method as
        ///     the
        ///     types are obviously rather vague.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="type">The type to get the native type for.</param>
        /// <returns>The native type.</returns>
        /// <exception cref="SolMarshallingException">Could not find a native type for the given type.</exception>
        [Obsolete]
        public static Type GetClosestNativeType(SolAssembly assembly, string type)
        {
            switch (type) {
                case SolValue.ANY_TYPE:
                case SolValue.CLASS_TYPE:
                case SolNil.TYPE:
                    return typeof(object);
                case SolNumber.TYPE:
                    return typeof(double);
                case SolBool.TYPE:
                    return typeof(bool);
                case SolString.TYPE:
                    return typeof(string);
                case SolFunction.TYPE:
                    return typeof(SolFunction.AutoDelegate);
                case SolTable.TYPE:
                    return typeof(Dictionary<object, object>);
                default: {
                    SolClassDefinition definition;
                    if (assembly.TryGetClass(type, out definition)) {
                        if (definition.DescribedType != null) {
                            return definition.DescribedType;
                        }
                        return typeof(object);
                    }
                    break;
                }
            }
            throw new SolMarshallingException("Cannot find a native type for SolType \"" + type + "\".");
        }*/

        /// <summary>Marshals the given native values to their SolValue representations. </summary>
        /// <param name="types">The types of the given <paramref name="values" />.</param>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <param name="values">The values to marshal.</param>
        /// <returns>The marshalled values.</returns>
        /// <exception cref="ArgumentException">Array lengths are not the same.</exception>
        /// <exception cref="SolMarshallingException">Failed to marshal the given value.</exception>
        public static SolValue[] MarshalFromNative(SolAssembly assembly, Type[] types, [ItemCanBeNull] object[] values)
        {
            if (types.Length != values.Length) {
                throw new ArgumentException($"You must provide an equal amount of values({values.Length}) and types({types.Length}).", nameof(types));
            }
            var array = new SolValue[types.Length];
            for (int i = 0; i < array.Length; i++) {
                array[i] = MarshalFromNative(assembly, types[i], values[i]);
            }
            return array;
        }

        /// <inheritdoc cref="MarshalFromNative(SolAssembly,Type,object)" />
        /// <remarks>
        ///     Keep in mind that the type of the value is inferred using <see cref="object.GetType()" /> which only
        ///     returns the most derived type.
        /// </remarks>
        /// <exception cref="SolMarshallingException">Failed to marshal the given value.</exception>
        [Obsolete("use generic  -- keep in mind to get the correct type and use type overload!!", true)]
        public static SolValue MarshalFromNative(SolAssembly assembly, [CanBeNull] object value)
        {
            return MarshalFromNative(assembly, value?.GetType() ?? typeof(void), value);
        }

        /// <summary>
        ///     Marshals the given native object to a <see cref="SolValue" /> useable in SolScript.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups.</param>
        /// <typeparam name="T">
        ///     The type of the native object.
        /// </typeparam>
        /// <param name="value">The value to marshal.</param>
        /// <returns>The marshalled <see cref="SolValue" />.</returns>
        /// <exception cref="SolMarshallingException">Failed to marshal the given value.</exception>
        public static SolValue MarshalFromNative<T>(SolAssembly assembly, [CanBeNull] T value)
        {
            return MarshalFromNative(assembly, typeof(T), value);
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
            foreach (ISolNativeMarshaller nativeMarshaller in s_NativeMarshallers) {
                if (nativeMarshaller.DoesHandle(assembly, type)) {
                    return nativeMarshaller.Marshal(assembly, value, type);
                }
            }
            /*if (type.IsClass) {
                AssemblyCache cache = GetAssemblyCache(assembly);
                SolClass solClass = cache.GetReference(value);
                if (solClass == null) {
                    SolClassDefinition classDef;
                    if (!assembly.TryGetClass(type, out classDef)) {
                        throw new SolMarshallingException($"Cannot marshal native type \"{type}\" to SolScript: This type does not have a SolClass representing it.");
                    }
                    try {
                        solClass = assembly.New(classDef, NativeClassCreationOptions);
                    } catch (SolTypeRegistryException ex) {
                        throw new SolMarshallingException(
                            $"Cannot marshal native type \"{type}\" to SolScript: A error occured while creating its representing class instance of type \"" + classDef.Type + "\".", ex);
                    }
                    // Assign the native object to all inheritance levels.
                    //SolClass.Inheritance inheritance = solClass.InheritanceChain;
                    DynamicReference described = new DynamicReference.FixedReference(value);
                    object descriptorObj;
                    solClass.DescribedObjectReference = described;
                    if (classDef.DescribedType == classDef.DescriptorType) {
                        solClass.DescriptorObjectReference = described;
                        descriptorObj = value;
                    } else {
                        descriptorObj = Activator.CreateInstance(classDef.DescriptorType);
                        solClass.DescriptorObjectReference = new DynamicReference.FixedReference(descriptorObj);
                    }
                    cache.StoreReference(value, solClass);
                    // Assigning self after storing in assembly cache.
                    SetSelf(value as INativeClassSelf, solClass);
                    if (!ReferenceEquals(descriptorObj, value)) {
                        SetSelf(descriptorObj as INativeClassSelf, solClass);
                    }
                }
                return solClass;
            }*/
            throw new SolMarshallingException(type, "No native marshaller has been registered for this type.");
        }

        /*private static void SetSelf(INativeClassSelf self, SolClass cls)
        {
            if (self != null) {
                if (self.Self != null) {
                    throw new SolMarshallingException("Type native Self value of native class \"" + self.GetType().Name + "\"(SolClass \"" + cls.Type
                                                      + "\") is not null. This is either an indicator for a duplicate native class or corrupted marshalling data.");
                }
                self.Self = cls;
            }
        }*/

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
                return new SolType(SolType.PrimitiveTypeNameOf(type), true);
            }
            foreach (ISolNativeMarshaller nativeMarshaller in s_NativeMarshallers) {
                if (nativeMarshaller.DoesHandle(assembly, type)) {
                    return nativeMarshaller.GetSolType(assembly, type);
                }
            }
            /*if (type.IsClass) {
                SolClassDefinition classDef;
                if (assembly.TryGetClass(type, out classDef)) {
                    return new SolType(classDef.Type, true);
                }
            }*/
            throw new SolMarshallingException(type, "No native marshaller has been registered for this type.");
        }

        internal static AssemblyCache GetAssemblyCache(SolAssembly assembly)
        {
            AssemblyCache cache;
            if (!assembly.TryGetMetaValue(SolMetaKeys.SolMarshalAssemblyCache, out cache)) {
                cache = new AssemblyCache();
                assembly.TrySetMetaValue(SolMetaKeys.SolMarshalAssemblyCache, cache);
            }
            return cache;
        }

        #region Nested type: AssemblyCache

        /// <summary>
        ///     The assembly cache is used to weakly store native objects(the descriptor object) and their
        ///     respective SolClass representations.
        /// </summary>
        internal class AssemblyCache
        {
#if !DO_NOT_CACHE_NATIVE
            /// <summary>
            ///     Creates a new assembly cache.
            /// </summary>
            public AssemblyCache()
            {
                m_NativeToSol = new ConditionalWeakTable<object, SolClass>( //100, 
                    //InternalHelper.ReferenceEqualityComparer<object>.Instance//*,
                    //InternalHelper.ReferenceEqualityComparer<SolClass>.Instance*/
                );
            }
#endif

#if !DO_NOT_CACHE_NATIVE
// The weakly stored objects and classes.
            private readonly ConditionalWeakTable<object, SolClass> m_NativeToSol;
#endif

            /// <summary>
            ///     Stores the class of a given native object.
            /// </summary>
            /// <param name="value">The native object.</param>
            /// <param name="solClass">The associated class.</param>
            public void StoreReference([NotNull] object value, [NotNull] SolClass solClass)
            {
                //Trace.WriteLine("Storing in assembly cache: " + value);
#if !DO_NOT_CACHE_NATIVE
// todo: determine if conditional weak table doesnt cause issues
// there was a reson why it was swapped for a third party one after all. (i assume. though sometimes i do things that just dont make sense...)
// note to self: keep in mind to document this kinda stuff....
                m_NativeToSol.Add(value, solClass);
#endif
            }

            /// <summary>
            ///     Gets the class representing the given object.
            /// </summary>
            /// <param name="value">The native object.</param>
            /// <returns>The class, or null.</returns>
            [CanBeNull]
            public SolClass GetReference([NotNull] object value)
            {
#if DO_NOT_CACHE_NATIVE
                return null;
#else
                SolClass solClass;
                SolClass obj = m_NativeToSol.TryGetValue(value, out solClass) ? solClass : null;
                //Debug.WriteLine("Getting cache of obj " + (value?.ToString() ?? "NULL") + " -> " + obj);
                return obj;
#endif
            }
        }

        #endregion

        #region Nested type: Comparer

        private class Comparer : IComparer<ISolNativeMarshaller>
        {
            private Comparer() {}
            public static readonly Comparer Instance = new Comparer();

            #region IComparer<ISolNativeMarshaller> Members

            /// <inheritdoc />
            public int Compare(ISolNativeMarshaller x, ISolNativeMarshaller y)
            {
                return PriorityComparer.Instance.Compare(x, y);
            }

            #endregion
        }

        #endregion
    }
}