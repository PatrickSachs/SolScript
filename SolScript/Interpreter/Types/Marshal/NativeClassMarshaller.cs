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
using SolScript.Exceptions;

namespace SolScript.Interpreter.Types.Marshal
{
    /// <summary>
    ///     This marshallers marshals native classes.
    /// </summary>
    public class NativeClassMarshaller : ISolNativeMarshaller
    {
        /// <summary>
        ///     The priority of the class marshaller.
        /// </summary>
        public const int PRIORITY = SolMarshal.PRIORITY_VERY_LOW - 50;

        /// <summary>
        ///     The options used to create new SolScript class instances when marshalling a native one.
        ///     <br />
        ///     We are not calling the constructor since this would create a new native object. Instead a sliently assign the
        ///     native value. Furthermore we enforce the creation since we wish to be able to e.g. create instances of annoation
        ///     classes.
        /// </summary>
        private static readonly ClassCreationOptions s_NativeClassCreationOptions = new ClassCreationOptions.Customizable()
            .SetEnforceCreation(true)
            .SetCallConstructor(false);

        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => PRIORITY;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            SolClassDefinition definition;
            return type.IsClass && assembly.TryGetClass(type, out definition);
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">The type does not have a class definition.</exception>
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            SolClassDefinition classDef;
            if (!assembly.TryGetClass(type, out classDef)) {
                throw new SolMarshallingException($"Cannot marshal native type \"{type}\" to SolScript: This type does not have a SolClass representing it.");
            }
            return new SolType(classDef.Type, true);
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">Failed to marshal the class instance.</exception>
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            if (value == null) {
                return SolNil.Instance;
            }
            SolMarshal.AssemblyCache cache = SolMarshal.GetAssemblyCache(assembly);
            SolClass solClass = cache.GetReference(value);
            if (solClass == null) {
                SolClassDefinition classDef;
                if (!assembly.TryGetClass(type, out classDef)) {
                    throw new SolMarshallingException($"Cannot marshal native type \"{type}\" to SolScript: This type does not have a SolClass representing it.");
                }
                try {
                    solClass = assembly.New(classDef, s_NativeClassCreationOptions);
                } catch (SolTypeRegistryException ex) {
                    throw new SolMarshallingException(
                        $"Cannot marshal native type \"{type}\" to SolScript: A error occured while creating its representing class instance of type \"" + classDef.Type + "\".", ex);
                }
                DynamicReference described = new DynamicReference.FixedReference(value);
                object descriptorObj;
                solClass.DescribedObjectReference = described;
                if (classDef.DescribedType == classDef.DescriptorType) {
                    solClass.DescriptorObjectReference = described;
                    descriptorObj = value;
                } else {
                    try {
                        descriptorObj = Activator.CreateInstance(classDef.DescriptorType);
                    } catch (Exception ex) {
                        throw new SolMarshallingException(type, classDef.Type, "An exception occured while trying to create the type descriptor for a marshaller class.", ex);
                    }
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
        }

        #endregion

        /// <exception cref="SolMarshallingException">An error occured.</exception>
        private static void SetSelf(INativeClassSelf self, SolClass cls)
        {
            if (self != null) {
                if (self.Self != null) {
                    throw new SolMarshallingException("Type native Self value of native class \"" + self.GetType().Name + "\"(SolClass \"" + cls.Type
                                                      + "\") is not null. This is either an indicator for a duplicate native class or corrupted marshalling data.");
                }
                self.Self = cls;
            }
        }
    }
}