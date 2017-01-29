﻿using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Statements {
    public class Statement_New : SolStatement {
        public Statement_New([NotNull] SolAssembly assembly, SolSourceLocation location, string typeName, params SolExpression[] arguments) : base(assembly, location) {
            Arguments = arguments;
            TypeName = typeName;
        }

        public readonly string TypeName;
        public readonly SolExpression[] Arguments;

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            var arguments = new SolValue[Arguments.Length];
            for (int i = 0; i < arguments.Length; i++) {
                // todo: execution context for native ctors
                arguments[i] = Arguments[i].Evaluate(context, parentVariables);
            }
            SolClass instance = Assembly.TypeRegistry.PrepareInstance(TypeName).Create(context, arguments);
            return instance;
        }

        protected override string ToString_Impl() {
            return
                $"new {TypeName}({InternalHelper.JoinToString(",", Arguments)})";
        }
    }
}