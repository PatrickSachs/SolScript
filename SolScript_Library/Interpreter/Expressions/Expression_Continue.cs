using System.Collections.Generic;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    public class Expression_Continue : TerminatingSolExpression
    {
        private Expression_Continue(SolAssembly assembly) : base(assembly, SolSourceLocation.Empty()) {}

        private static readonly Dictionary<SolAssembly, Expression_Continue> s_lookup = new Dictionary<SolAssembly, Expression_Continue>();

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            terminators = Terminators.Continue;
            return SolNil.Instance;
        }

        protected override string ToString_Impl()
        {
            return "continue";
        }

        #endregion

        public static Expression_Continue InstanceOf(SolAssembly assembly)
        {
            Expression_Continue instance;
            if (s_lookup.TryGetValue(assembly, out instance)) {
                return instance;
            }
            return s_lookup[assembly] = new Expression_Continue(assembly);
        }
    }
}