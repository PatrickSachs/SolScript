using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_DeclareVar : SolStatement {
        public Statement_DeclareVar([NotNull] SolAssembly assembly, SourceLocation location/*, bool local,*/, string name, SolType type, SolExpression valueGetter) : base(assembly, location) {
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

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            SolValue value = ValueGetter?.Evaluate(context, parentVariables) ?? SolNil.Instance;
            parentVariables.Declare(Name, Type);
            parentVariables.Assign(Name, value);
            return value;
        }

        protected override string ToString_Impl() {
            return $"var {Name} : {Type} = {ValueGetter}";
        }

        #endregion
    }
}