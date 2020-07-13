using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types {
    public abstract class SolFunction : SolValue, ISourceLocateable {
        public SolFunction(SolAssembly assembly, SourceLocation location, SolType returnType, bool allowOptionalParams,[ItemNotNull] params SolParameter[] parameters) {
            Id = s_NextId++;
            Location = location;
           // ParentVariables = parentVariables;
            Assembly = assembly;
            Return = returnType;
            ParameterAllowOptional = allowOptionalParams;
            Parameters = parameters;
        }

       // public IVariables ParentVariables;
        private static uint s_NextId;
        public readonly uint Id;
        public bool ParameterAllowOptional { get; protected set; }
        public SolParameter[] Parameters { get; protected set; }
        public readonly SolAssembly Assembly;
        public SolType Return { get; protected set; }

        public const string TYPE = "function";

        public override string Type => TYPE;

        #region ISourceLocateable Members

        public SourceLocation Location { get; set; }

        #endregion

        public abstract SolValue Call(SolExecutionContext context, SolClass instance, params SolValue[] args);

        public override bool IsEqual(SolExecutionContext context, SolValue other) {
            SolFunction otherFunc = other as SolFunction;
            return otherFunc != null && Id == otherFunc.Id;
        }
    }
}