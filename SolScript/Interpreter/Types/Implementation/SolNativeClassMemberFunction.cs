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
using System.Reflection;
using JetBrains.Annotations;
using SolScript.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This <see cref="SolFunction" /> is used for native class functions in SolScript.
    /// </summary>
    public sealed class SolNativeClassMemberFunction : SolNativeClassFunction
    {
        /// <summary>
        ///     Creates a new native instance function from the given parameters.
        /// </summary>
        /// <param name="definedIn">The class instance this function belongs to.</param>
        /// <param name="definition">The definition of this function.</param>
        public SolNativeClassMemberFunction([NotNull] IClassLevelLink definedIn, SolFunctionDefinition definition) : base(definedIn, definition) {}

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            //SolClass.Inheritance inheritance = ClassInstance.FindInheritance(Definition.DefinedIn);
            SolClass.Inheritance inheritance = DefinedIn.Inheritance();
            /*if (inheritance == null) {
                throw new InvalidOperationException($"Internal error: Failed to find inheritance on class instance \"{DefinedIn}\".");
            }*/
            object[] values;
            try {
                values = ParameterInfo.Marshal(context, args);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Could to marshal the function parameters to native objects: " + ex.Message, ex);
            }
            MethodInfo nativeMethod = Definition.Chunk.GetNativeMethod();
            object nativeMember;
            if (!DefinedIn.ClassInstance.DescriptorObjectReference.TryGet(out nativeMember)) {
                throw new InvalidOperationException("Internal error: The internal reference of class inheritance \""
                                                    + inheritance.Definition.Type + "\" in class \"" + DefinedIn
                                                    + "\" could not be resolved.");
            }
            object nativeObject = InternalHelper.SandboxInvokeMethod(context, Definition.Chunk.GetNativeMethod(), nativeMember, values);
            SolValue returnValue;
            try {
                returnValue = SolMarshal.MarshalFromNative(Assembly, nativeMethod.ReturnType, nativeObject);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, "Failed to marshal the return value(Native Type: \""
                                                       + (nativeObject?.GetType().Name ?? "null") + "\") to SolScript.", ex);
            }
            return returnValue;
        }

        #endregion
    }
}