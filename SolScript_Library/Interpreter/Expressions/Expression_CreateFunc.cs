using System.Collections.Generic;
using Irony.Parsing;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Expressions {
    public class Expression_CreateFunc : SolExpression {
        public SolChunk Chunk;
        public bool ParameterAllowOptional;
        public SolParameter[] Parameters;
        public SolType Type;

        public override SolValue Evaluate(SolExecutionContext context)
        {
            context.CurrentLocation = Location;
            SolScriptFunction function = new SolScriptFunction(Location, context.VariableContext) {
                Return = Type,
                Parameters = Parameters,
                ParameterAllowOptional = ParameterAllowOptional,
                Chunk = Chunk
            };
            //context.EvaluationStack.Push(function);
            return function;
        }

        protected override string ToString_Impl() {
            return
                $"Expression_CreateFunc(Type={Type}, Parameters=[{string.Join(",", (IEnumerable<SolParameter>) Parameters)}" +
                (ParameterAllowOptional ? (Parameters.Length == 0 ? "..." : ", ...") : "") + $"], Chunk={Chunk})";
        }

        public Expression_CreateFunc(SourceLocation location) : base(location) {
        }
    }
}