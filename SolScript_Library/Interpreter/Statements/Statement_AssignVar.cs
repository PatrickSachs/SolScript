using Irony.Parsing;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Statements {
    public class Statement_AssignVar : SolStatement {
        //public SolParameter Variable;
        public TargetRef Target;
        public SolExpression ValueGetter;

        public override SolValue Execute(SolExecutionContext context)
        {
            context.CurrentLocation = Location;
            SolValue value = ValueGetter.Evaluate(context);
            Target.Set(value, context);
            return value;
        }

        protected override string ToString_Impl() {
            return $"Statement_AssignVar(Target={Target}, ValueGetter={ValueGetter})";
        }

        #region Nested type: VarTarget

        public abstract class TargetRef {
            public abstract void Set(SolValue value, SolExecutionContext context);
        }

        #endregion

        #region Nested type: VarTarget_Table

        public class IndexedVariable : TargetRef {
            public SolExpression KeyGetter;
            public SolExpression TableGetter;

            /// <remarks> Evaluates the TableGetter, then the KeyGetter </remarks>
            public override void Set(SolValue value, SolExecutionContext context) {
                SolValue indexableRaw = TableGetter.Evaluate(context);
                // TODO: IValueIndexable for classes?
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable == null) {
                    throw new SolScriptInterpreterException(context.CurrentLocation + " : Tried to set an indexed value to a " + indexableRaw.Type +
                                                            " value. This type cannot be indexed.");
                }
                indexable[KeyGetter.Evaluate(context)] = value;
            }

            public override string ToString() {
                return $"IndexedVariable(TableGetter={TableGetter}, KeyGetter={KeyGetter})";
            }
        }

        #endregion

        #region Nested type: VarTarget_Variable

        public class NamedVariable : TargetRef {
            //public bool Local;
            public string Name;
            //public SolType Type;

            public override void Set(SolValue value, SolExecutionContext context) {
                context.VariableContext.AssignValue(Name, value);
            }

            public override string ToString() {
                return $"NamedVariable({Name})";
            }
        }

        #endregion

        public Statement_AssignVar(SourceLocation location) : base(location) {
        }
    }
}