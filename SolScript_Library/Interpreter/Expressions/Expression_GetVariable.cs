using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Expressions {
    public class Expression_GetVariable : SolExpression {
        public Expression_GetVariable(SolAssembly assembly, SourceLocation location) : base(assembly, location) {
        }

        public SourceRef Source;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables) {
            context.CurrentLocation = Location;
            return Source.Get(context, parentVariables);
        }

        protected override string ToString_Impl() {
            return Source.ToString();
        }

        #endregion

        #region Nested type: IndexedVariable

        public class IndexedVariable : SourceRef {
            public IndexedVariable(SourceLocation location) : base(location) {
            }

            public SolExpression KeyGetter;
            public SolExpression TableGetter;

            #region Overrides

            public override SolValue Get(SolExecutionContext context, IVariables parentVariables) {
                context.CurrentLocation = Location;
                SolValue indexableRaw = TableGetter.Evaluate(context, parentVariables);
                SolValue key = KeyGetter.Evaluate(context, parentVariables);
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable == null) {
                    throw SolScriptInterpreterException.IllegalAccessType(context, indexableRaw.Type,
                        "This type cannot be indexed.");
                }
                //SolDebug.WriteLine("index " + indexable + " by " + key);
                SolValue value = indexable[key];
                return value;
            }

            public override string ToString() {
                return $"{TableGetter}[{KeyGetter}]";
            }

            #endregion
        }

        #endregion

        #region Nested type: NamedVariable

        public class NamedVariable : SourceRef {
            public NamedVariable(SourceLocation location) : base(location) {
            }

            public string Name;

            #region Overrides

            public override SolValue Get(SolExecutionContext context, IVariables parentVariables) {
                context.CurrentLocation = Location;
                SolValue rawValue = parentVariables.Get(Name);
                if (rawValue == null) {
                    throw SolScriptInterpreterException.IllegalAccessName(context, Name,
                        "This variable has not been assigned.");
                }
                return rawValue;
            }

            /*public string o([CanBeNull]string a) {
                string b = "b";
                return a ?? "c" + b;
            }*/
            public override string ToString() {

                return $"{Name}";
            }

            #endregion
        }

        #endregion

        #region Nested type: SourceRef

        public abstract class SourceRef {
            public SourceRef(SourceLocation location) {
                Location = location;
            }

            public readonly SourceLocation Location;
            public abstract SolValue Get(SolExecutionContext context, IVariables parentVariables);
        }

        #endregion
    }
}