using Irony.Parsing;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Expressions {
    public class Expression_GetVariable : SolExpression {
        public SourceRef Source;

        public override SolValue Evaluate(SolExecutionContext context)
        {
            context.CurrentLocation = Location;
            return Source.Get(context);
        }

        protected override string ToString_Impl() {
            return $"Expression_GetVariable({Source})";
        }

        #region Nested type: SourceRef

        public abstract class SourceRef {
            public SourceRef(SourceLocation location) {
                Location = location;
            }

            public readonly SourceLocation Location;
            public abstract SolValue Get(SolExecutionContext context);
        }

        #endregion

        #region Nested type: NamedVariable

        public class NamedVariable : SourceRef {
            public string Name;

            public override SolValue Get(SolExecutionContext context) {
                //SolDebug.WriteLine("EVAL VAR " + Name);
                SolValue rawValue = context.VariableContext.GetValue_X(Name);
                if (rawValue == null) {
                    throw new SolScriptInterpreterException(Location + " : Tried to access non-assigned variable " + Name + ".");
                }
                return rawValue;
            }

            public override string ToString() {
                return $"NamedVariable({Name})";
            }

            public NamedVariable(SourceLocation location) : base(location) {
            }
        }

        #endregion

        #region Nested type: IndexedVariable

        public class IndexedVariable : SourceRef {
            public SolExpression KeyGetter;
            public SolExpression TableGetter;

            public override SolValue Get(SolExecutionContext context)
            {
                SolValue indexableRaw = TableGetter.Evaluate(context);
                SolValue key = KeyGetter.Evaluate(context);
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable == null)
                {
                    throw new SolScriptInterpreterException(context.CurrentLocation + " : Tried to get an indexed value from a " + indexableRaw.Type +
                                                            " value. This type cannot be indexed.");
                }
                return indexable[key];
            }

            public override string ToString() {
                return $"IndexedVariable(TableGetter={TableGetter}, KeyGetter={KeyGetter})";
            }

            public IndexedVariable(SourceLocation location) : base(location) {
            }
        }

        #endregion

        public Expression_GetVariable(SourceLocation location) : base(location) {
        }
    }
}