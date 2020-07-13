using Irony.Parsing;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Statements {
    public class Statement_AssignVar : SolStatement {
        public TargetRef Target;
        public SolExpression ValueGetter;

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            SolValue value = ValueGetter.Evaluate(context, parentVariables);
            Target.Set(value, context, parentVariables);
            return value;
        }

        protected override string ToString_Impl() {
            return $"{Target} = {ValueGetter}";
        }

        #region Nested type: VarTarget

        public abstract class TargetRef {
            public abstract void Set(SolValue value, SolExecutionContext context, IVariables parentVariables);
        }

        #endregion

        #region Nested type: VarTarget_Table

        public class IndexedVariable : TargetRef {
            public SolExpression KeyGetter;
            public SolExpression TableGetter;

            /// <remarks> Evaluates the TableGetter, then the KeyGetter </remarks>
            public override void Set(SolValue value, SolExecutionContext context, IVariables parentVariables) {
                SolValue indexableRaw = TableGetter.Evaluate(context, parentVariables);
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable == null) {
                    throw SolScriptInterpreterException.IllegalAccessType(context, indexableRaw.Type,
                        "This type cannot be indexed.");
                }
                indexable[KeyGetter.Evaluate(context, parentVariables)] = value;
            }

            public override string ToString() {
                return $"{TableGetter}[{KeyGetter}]";
            }
        }

        #endregion

        #region Nested type: VarTarget_Variable

        public class NamedVariable : TargetRef {
            public string Name;

            public override void Set(SolValue value, SolExecutionContext context, IVariables parentVariables) {
                parentVariables.Assign(Name, value);
            }

            public override string ToString() {
                return Name;
            }
        }

        #endregion

        public Statement_AssignVar(SolAssembly assembly, SourceLocation location) : base(assembly, location) {
        }
    }
}