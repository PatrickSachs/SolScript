using System;
using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Expressions {
    public class Expression_CreateFunc : SolExpression {
        public SolChunk Chunk;
        public bool ParameterAllowOptional;
        public SolParameter[] Parameters;
        public SolType Type;

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            // issue: create non class function
            // todo: create func expression (lambdas?)
            throw new NotImplementedException();
            /*context.CurrentLocation = Location;
            SolScriptClassFunction function = new SolScriptClassFunction(Assembly, Location) {
                Return = Type,
                Parameters = Parameters,
                ParameterAllowOptional = ParameterAllowOptional,
                Chunk = Chunk
            };
            return function;*/
        }

        protected override string ToString_Impl() {
            return
                $"Expression_CreateFunc(Type={Type}, Parameters=[{string.Join(",", (IEnumerable<SolParameter>) Parameters)}" +
                (ParameterAllowOptional ? (Parameters.Length == 0 ? "..." : ", ...") : "") + $"], Chunk={Chunk})";
        }
        
        public Expression_CreateFunc([NotNull] SolAssembly assembly, SourceLocation location) : base(assembly, location) {
        }
    }
}