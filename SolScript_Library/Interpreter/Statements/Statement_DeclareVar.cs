using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements
{
    public class Statement_DeclareVar : SolStatement
    {
        public Statement_DeclareVar([NotNull] SolAssembly assembly, SolSourceLocation location, string name, SolType type, [CanBeNull] SolExpression valueGetter)
            : base(assembly, location)
        {
            //Local = local;
            Name = name;
            Type = type;
            ValueGetter = valueGetter;
        }

        //public readonly bool Local;
        public readonly string Name;
        public readonly SolType Type;
        [CanBeNull] public readonly SolExpression ValueGetter;

        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            terminators = Terminators.None;
            parentVariables.Declare(Name, Type);
            if (ValueGetter != null) {
                SolValue value = ValueGetter.Evaluate(context, parentVariables);
                parentVariables.Assign(Name, value);
                return value;
            }
            return SolNil.Instance;
        }

        protected override string ToString_Impl()
        {
            string middle = ValueGetter != null ? " = " + ValueGetter : string.Empty;
            return $"var {Name} : {Type}{middle}";
        }

        #endregion
    }
}