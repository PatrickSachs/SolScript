using System.Collections.Generic;
using Irony.Parsing;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions {
    public class Expression_Break : TerminatingSolExpression {
        private Expression_Break(SolAssembly assembly) : base(assembly, SourceLocation.Empty) {
        }
        private static Dictionary<SolAssembly, Expression_Break> s_lookup = new Dictionary<SolAssembly, Expression_Break>();

        public static Expression_Break InstanceOf(SolAssembly assembly)
        {
            Expression_Break instance;
            if (s_lookup.TryGetValue(assembly, out instance)) return instance;
            return (s_lookup[assembly] = new Expression_Break(assembly));
        }
        
        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables) {
            return SolNil.Instance;
        }

        protected override string ToString_Impl() {
            return "break";
        }

        #endregion

        public override Terminators Terminators => Terminators.Break;
    }
}