using System;
using SolScript.Interpreter.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     This class is used for class functions declared in code.
    /// </summary>
    public sealed class SolScriptClassFunction : SolClassFunction
    {
        /// <summary>
        ///     Creates a new function instance.
        /// </summary>
        /// <param name="inClass">The class this function belongs to.</param>
        /// <param name="definition">The definition of this function.</param>
        public SolScriptClassFunction(SolClass inClass, SolFunctionDefinition definition)
        {
            ClassInstance = inClass;
            Definition = definition;
        }

        /// <inheritdoc />
        public override SolAssembly Assembly {
            get {
                if (Definition.DefinedIn != null) {
                    return Definition.DefinedIn.Assembly;
                }
                throw new InvalidOperationException("Could not get the defining class of a class function.");
            }
        }

        /// <inheritdoc />
        public override SolFunctionDefinition Definition { get; }

        /// <inheritdoc />
        public override SolClass ClassInstance { get; }

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            SolClass.Inheritance inheritance = ClassInstance.FindInheritance(Definition.DefinedIn).NotNull();
            Variables varContext = new Variables(Assembly) {
                Parent = inheritance.Variables // todo: orginally this was parented to the internal variables? was this a mistake or does it mave a reason?
            };
            try {
                InsertParameters(varContext, args);
            } catch (SolVariableException ex) {
                throw SolRuntimeException.InvalidFunctionCallParameters(context, ex);
            }
            // Functions pretty much eat the terminators since that's what the terminators are supposed to terminate down to.
            Terminators terminators;
            SolValue returnValue = Definition.Chunk.GetScriptChunk().Execute(context, varContext, out terminators);
            return returnValue;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 11 + (int) Id;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return other == this;
        }

        #endregion
    }
}