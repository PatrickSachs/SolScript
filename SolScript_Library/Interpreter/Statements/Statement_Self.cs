using System;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     The self-Statement can be used to access the variables of the current class. <br />The reasons to use self over
    ///     direct identifier access is the possibility to index the own class by using expressions, or to access
    ///     fields/functions with the same name as variable.
    /// </summary>
    public class Statement_Self : SolStatement, IWrittenInClass
    {
        /// <inheritdoc />
        public Statement_Self(SolAssembly assembly, SolSourceLocation location, string writtenInClass, SolExpression indexer) : base(assembly, location)
        {
            Indexer = indexer;
            WrittenInClass = writtenInClass;
        }

        /// <summary>
        ///     The expression returning the index.
        /// </summary>
        public readonly SolExpression Indexer;

        #region IWrittenInClass Members

        /// <inheritdoc />
        public string WrittenInClass { get; }

        #endregion

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Self statement cannot be executed on a null current class.</exception>
        /// <exception cref="SolRuntimeException">Failed to resolve variable.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            terminators = Terminators.None;
            SolClass currentClass = context.CurrentClass;
            if (currentClass == null) {
                throw new InvalidOperationException("self statement cannot be executed on a null current class.");
            }
            SolClass.Inheritance inheritance = currentClass.FindInheritance(WrittenInClass);
            if (inheritance == null) {
                throw new SolRuntimeException(context,
                    "Tried to access class \"" + currentClass.Type + "\" on inheritance level \"" + WrittenInClass + "\". The class does not contain said inheritance.");
            }
            SolValue indexerRaw = Indexer.Evaluate(context, parentVariables);
            SolString index = indexerRaw as SolString;
            if (index == null) {
                throw new SolRuntimeException(context, "Cannot index self by a \"" + indexerRaw.Type + "\" value. Only strings can be used.");
            }
            try {
                return inheritance.GetVariables(SolAccessModifier.Local, SolClass.Inheritance.Mode.All).Get(index.Value);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to resolve a self statement variable.", ex);
            }
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return "Statement_Self(WrittenInClass=" + WrittenInClass + ", Indexer=" + Indexer + ")";
        }

        #endregion
    }
}