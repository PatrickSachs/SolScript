using System;
using System.Collections.Generic;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;

namespace SolScript.Interpreter.Expressions
{
    public class Expression_CreateFunc : SolExpression
    {
        public Expression_CreateFunc(SolAssembly assembly, SolSourceLocation location, SolChunk chunk, SolType type, bool parameterAllowOptional, params SolParameter[] parameters)
            : base(assembly, location)
        {
            Chunk = chunk;
            Type = type;
            Parameters = new SolParameterInfo(parameters, parameterAllowOptional);
        }

        public readonly SolChunk Chunk;
        public readonly SolParameterInfo Parameters;
        public readonly SolType Type;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            // todo: better way for lamda to capture the parent variables (done by compiler to scan for needed ones?)
            return new SolScriptLamdaFunction(Assembly, Location, Parameters, Type, Chunk, parentVariables);
        }

        protected override string ToString_Impl()
        {
            return
                $"Expression_CreateFunc(Type={Type}, Parameters={Parameters}, Chunk={Chunk})";
        }

        #endregion
    }
}