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
    ///     This marshaller explictly handles the object type by unwrapping the actual type of the value from the instance
    ///     itself.
    ///     <br />
    ///     However keep in mind that exposing object values to SolScript is dangerous. SolScript will not be able to check
    ///     ahead of time if those values can or cannot be marshalled to SolScript and thus raise the possibility for a
    ///     <see cref="SolMarshallingException" /> at runtime.
    /// </summary>
    public class NativeObjectMarshaller : ISolNativeMarshaller
    {
        /// <summary>
        ///     The marshaller priority.
        /// </summary>
        public const int PRIORITY = NativeClassMarshaller.PRIORITY - 50;

        #region ISolNativeMarshaller Members

        /// <inheritdoc />
        public int Priority => PRIORITY;

        /// <inheritdoc />
        public bool DoesHandle(SolAssembly assembly, Type type)
        {
            return type == typeof(object);
        }

        /// <inheritdoc />
        public SolType GetSolType(SolAssembly assembly, Type type)
        {
            return SolType.AnyNil;
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException">Cannot marshal.</exception>
        public SolValue Marshal(SolAssembly assembly, object value, Type type)
        {
            if (value == null) {
                return SolNil.Instance;
            }
            Type actualType = value.GetType();
            // Ensure that we aren't actually an object in order to prevent a stack overflow. Users can still
            // marshall the object class itself by providing a descriptor for it.
            if (actualType == typeof(object)) {
                throw new SolMarshallingException(type, "Cannot marshal raw native object instances. Either create a wrapper for the object type or don't expose object members.");
            }
            // And give it a second go.
            return SolMarshal.MarshalFromNative(assembly, actualType, value);
        }

        #endregion
    }
}