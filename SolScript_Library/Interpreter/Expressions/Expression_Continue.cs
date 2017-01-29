using System.Collections.Generic;
using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Continue : TerminatingSolExpression {
        private Expression_Continue(SolAssembly assembly) : base(assembly, SolSourceLocation.Empty()) {
        }

        private static Dictionary<SolAssembly, Expression_Continue> s_lookup = new Dictionary<SolAssembly, Expression_Continue>();

        public static Expression_Continue InstanceOf(SolAssembly assembly) {
            Expression_Continue instance;
            if (s_lookup.TryGetValue(assembly, out instance)) return instance;
            return (s_lookup[assembly] = new Expression_Continue(assembly));
        }

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables) {
            return SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return "continue";
        }

        #endregion

        public override Terminators Terminators => Terminators.Continue;
    }
}