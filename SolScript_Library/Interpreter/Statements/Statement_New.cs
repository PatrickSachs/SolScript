using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_New : SolStatement {
        public Statement_New([NotNull] SolAssembly assembly, SourceLocation location, string typeName, params SolExpression[] arguments) : base(assembly, location) {
            Arguments = arguments;
            TypeName = typeName;
        }

        public readonly string TypeName;
        public readonly SolExpression[] Arguments;

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            var arguments = new SolValue[Arguments.Length];
            for (int i = 0; i < arguments.Length; i++) {
                arguments[i] = Arguments[i].Evaluate(context, parentVariables);
            }
            SolClass instance = Assembly.TypeRegistry.CreateInstance(TypeName, false, arguments).Create(context);
            return instance;
        }

        protected override string ToString_Impl() {
            return
                $"new {TypeName}({InternalHelper.JoinToString(",", Arguments)})";
        }
    }
}