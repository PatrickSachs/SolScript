using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    public class Statement_DeclareVar : SolStatement
    {
        public Statement_DeclareVar([NotNull] SolAssembly assembly, SolSourceLocation location, string name, SolType type, [CanBeNull] SolExpression valueGetter)
            : base(assembly, location)
        {
            Name = name;
            Type = type;
            ValueGetter = valueGetter;
        }

        public readonly string Name;
        public readonly SolType Type;
        [CanBeNull] public readonly SolExpression ValueGetter;

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">An error occured while trying to assign declare the variable.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            terminators = Terminators.None;
            try {
                parentVariables.Declare(Name, Type);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to declare the variable \"" + Name + "\".", ex);
            }
            if (ValueGetter != null) {
                SolValue value = ValueGetter.Evaluate(context, parentVariables);
                try {
                    parentVariables.Assign(Name, value);
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, "Failed to assign the initial value to the variable \"" + Name + "\".", ex);
                }
                return value;
            }
            return SolNil.Instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            string middle = ValueGetter != null ? " = " + ValueGetter : string.Empty;
            return $"var {Name} : {Type}{middle}";
        }

        #endregion
    }
}