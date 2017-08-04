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
using System.Runtime.Serialization;
using SolScript.Interpreter;

namespace SolScript.Exceptions
{
    /// <summary>
    ///     The SolRuntimeException is used to represent an exception that occured during the actual run of an assembly.
    /// </summary>
    [Serializable]
    public class SolRuntimeException : SolException
    {
        /// <summary>
        ///     Creates a new runtime exception.
        /// </summary>
        /// <param name="context">The context. - Required for generating the stack trace.</param>
        /// <param name="message">The exception message.</param>
        public SolRuntimeException(SolExecutionContext context, string message) : base(context.CurrentLocation, message)
        {
            SolStackTrace = context.GenerateStackTrace();
            SourceContext = context.Id;
        }

        /// <inheritdoc />
        /// <exception cref="SerializationException">
        ///     The class name is null or <see cref="P:System.Exception.HResult" /> is zero
        ///     (0).
        /// </exception>
        /// <exception cref="InvalidCastException">A value cannot be converted to a required type. </exception>
        protected SolRuntimeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            SolStackTrace = info.GetString(STACK);
            SourceContext = info.GetUInt32(SOURCE);
        }

        /// <summary>
        ///     Creates a new runtime exception and wraps an exception.
        /// </summary>
        /// <param name="context">The context. - Required for generating the stack trace.</param>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The wrapped exception.</param>
        public SolRuntimeException(SolExecutionContext context, string message, Exception inner) : base(
            context.CurrentLocation, message, inner)
        {
            SolStackTrace = context.GenerateStackTrace();
            SourceContext = context.Id;
        }

        private const string STACK = "SolRuntimeException.StackTrace";
        private const string SOURCE = "SolRuntimeException.SourceContext";

        /// <summary>
        ///     The exception message without stack trace or file location.
        /// </summary>
        public string SolStackTrace { get; }

        /// <summary>
        ///     The source context id.
        /// </summary>
        internal uint SourceContext { get; }

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SerializationException">A value has already been associated with a name. </exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(STACK, StackTrace);
            info.AddValue(SOURCE, SourceContext);
        }

        #endregion
    }
}