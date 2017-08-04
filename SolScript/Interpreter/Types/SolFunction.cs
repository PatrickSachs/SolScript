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
using JetBrains.Annotations;
using NodeParser;
using PSUtility.Strings;
using SolScript.Exceptions;
using SolScript.Interpreter.Types.Marshal;
using SolScript.Properties;

namespace SolScript.Interpreter.Types
{
    /// <summary>
    ///     This is the base class for all function in SolScript. Only use more derived type if you know exactly what you are
    ///     doing.<br />
    ///     The <see cref="SolFunction" /> class itself allows you to easily call a function with some arguments and obtain its
    ///     return value.
    /// </summary>
    public abstract class SolFunction : SolValue, ISourceLocateable
    {
        #region Delegates

        /// <summary>
        ///     A delegate used to represent SolFunctions.
        /// </summary>
        /// <param name="arguments">The arguments to the function call.</param>
        /// <returns>The return value.</returns>
        [NotNull]
        public delegate SolValue DirectDelegate([ItemNotNull] params SolValue[] arguments);

        #endregion

        // SolFunction may not be extended by other assemblies.
        internal SolFunction()
        {
            Id = s_NextId++;
        }

        /// <summary>
        ///     A <see cref="SolFunction" /> is always of type "function".
        /// </summary>
        public const string TYPE = "function";

        // The Id the next function will receive.
        private static uint s_NextId;

        /// <summary>
        ///     The unique Id of this function.
        /// </summary>
        public readonly uint Id;

        /// <summary>
        ///     The <see cref="SolAssembly" /> this function belongs to.
        /// </summary>
        public abstract SolAssembly Assembly { get; }

        /// <summary>
        ///     The class and the level this function was defined in. Can be null for global functions.
        /// </summary>
        /// <remarks>
        ///     WARNING: If a function returns a value from this property this does NOT mean that the function can also
        ///     omnidirectionally be
        ///     obtained from the class instance itself since function such as e.g. lamda functions may be defined in a certain
        ///     class but are not a member of said class.
        /// </remarks>
        [CanBeNull]
        public abstract IClassLevelLink DefinedIn { get; }

        /// <summary>
        ///     The parameters of this function.
        /// </summary>
        public abstract SolParameterInfo ParameterInfo { get; }

        /// <summary>
        ///     The return type of this function.
        /// </summary>
        public abstract SolType ReturnType { get; }

        /// <inheritdoc cref="TYPE" />
        public override string Type => TYPE;

        #region ISourceLocateable Members

        /// <inheritdoc />
        public abstract NodeLocation Location { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override bool IsReferenceEqual(SolExecutionContext context, SolValue other)
        {
            return Id == (other as SolFunction)?.Id;
        }

        /// <inheritdoc />
        /// <remarks>One function equals another if they share the same <see cref="Id" />.</remarks>
        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            SolFunction otherFunc = other as SolFunction;
            return otherFunc != null && Id == otherFunc.Id;
        }

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<" + (DefinedIn?.ToString() ?? "global") + ">";
        }

        /// <summary>
        /// The name of the function. This may be a rather abstract name in the case of lamdas or a concrete name in the case of defined functions. The ToString() version is more explicit for debugging.
        /// </summary>
        public virtual string Name => ToString_Impl(null);

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            // We have a special delegate for direct calls which saves us from having 
            // to create costly reflections stuff to build and marshal the delegate.
            if (type == typeof(DirectDelegate)) {
                return (DirectDelegate) (arguments => Call(new SolExecutionContext(Assembly, "Direct native delegate call"), arguments));
            }
            if (typeof(Delegate).IsAssignableFrom(type)) {
                return NativeDelegateMarshaller.CreateDelegate(type, this);
            }
            return base.ConvertTo(type);
        }

        #endregion

        /// <summary>
        ///     Calls the function.
        /// </summary>
        /// <param name="context">The exceution context to call the function in.</param>
        /// <param name="args">The arguments for the function. The arguments will be verified by the function.</param>
        /// <returns>The return value of the function.</returns>
        /// <exception cref="SolRuntimeException">An error occured while calling this function.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured while calling this function.</exception>
        [NotNull]
        public SolValue Call([NotNull] SolExecutionContext context, [ItemNotNull] params SolValue[] args)
        {
            SolValue returnValue;
            context.PushClassEntry(DefinedIn != null
                ? SolClassEntry.Class(DefinedIn.ClassInstance, DefinedIn.InheritanceLevel)
                : SolClassEntry.Global());
            {
                SolDebug.WriteLine("Pushing from " + context.Name + "#" + context.Id);
                context.PushStackFrame(this);
                {
                    try {
                        args = ParameterInfo.VerifyArguments(Assembly, args);
                    } catch (SolVariableException ex) {
                        throw new SolRuntimeException(context, Resources.Err_InvalidFunctionCallParameters.FormatWith(Name), ex);
                    }
                    returnValue = Call_Impl(context, args);
                    if (!ReturnType.IsCompatible(Assembly, returnValue.Type)) {
                        throw new SolRuntimeException(context, $"Expected a return value of type \"{ReturnType}\", recceived a value of type \"{returnValue.Type}\".");
                    }
                }
                context.PopStackFrame();
            }
            context.PopClassEntry();
            return returnValue;
        }

        /// <inheritdoc cref="Call" />
        protected abstract SolValue Call_Impl([NotNull] SolExecutionContext context, [ItemNotNull] params SolValue[] args);

        /// <summary>
        ///     Inserts the given arguments into the variables. The name of the variables is used according to the ones defined in
        ///     the <see cref="ParameterInfo" />.
        /// </summary>
        /// <param name="variables">The variables to insert the arguments into.</param>
        /// <param name="arguments">The arguments to insert.</param>
        /// <remarks>
        ///     No further type/length checks will be performed. It is your responsibility to make sure that the types are of the
        ///     correct type. <br />This should typically be no big issue since the default arguments passed to the function have
        ///     already
        ///     been verified. <br />Should you override the arguments manually for some reason you may want to re-check the types
        ///     to
        ///     avoid critical bugs in user code.
        /// </remarks>
        /// <exception cref="SolVariableException">An error occured during insertion.</exception>
        protected virtual void InsertParameters(IVariables variables, SolValue[] arguments)
        {
            for (int i = 0; i < ParameterInfo.Count; i++) {
                SolParameter parameter = ParameterInfo[i];
                variables.Declare(parameter.Name, parameter.Type);
                variables.Assign(parameter.Name, arguments.Length > i ? arguments[i] : SolNil.Instance);
            }
            // Additional arguments
            if (ParameterInfo.AllowOptional) {
                variables.Declare("args", new SolType("table", false));
                SolTable argsTable = new SolTable();
                for (int i = ParameterInfo.Count; i < arguments.Length; i++) {
                    argsTable.Append(arguments[i]);
                }
                variables.Assign("args", argsTable);
            }
        }
    }
}