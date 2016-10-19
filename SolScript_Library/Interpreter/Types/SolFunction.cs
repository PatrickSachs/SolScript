using Irony.Parsing;

namespace SolScript.Interpreter.Types {
    public abstract class SolFunction : SolValue, ISourceLocateable {
        public SolFunction(SourceLocation location, VarContext parentContext) {
            Id = s_NextId++;
            Location = location;
            ParentContext = parentContext;
        }

        public static readonly SolType MarshalFromType = new SolType("function", false);
        public static readonly SolType MarshalToType = new SolType("function", true);
        private static uint s_NextId;
        public readonly uint Id;
        public bool ParameterAllowOptional;
        public SolParameter[] Parameters;

        public VarContext ParentContext;
        public SolType Return;

        public override string Type { get; protected set; } = "function";

        #region ISourceLocateable Members

        public SourceLocation Location { get; set; }

        #endregion

        public abstract SolValue Call(SolValue[] args, SolExecutionContext context);

        public override bool IsEqual(SolValue other) {
            SolFunction otherFunc = other as SolFunction;
            return otherFunc != null && Id == otherFunc.Id;
        }
    }
}