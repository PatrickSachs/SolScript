using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Statements
{
    public class Statement_AssignVar : SolStatement
    {
        public Statement_AssignVar(SolAssembly assembly, SolSourceLocation location, TargetRef target, SolExpression valueGetter) : base(assembly, location)
        {
            Target = target;
            ValueGetter = valueGetter;
        }

        public readonly TargetRef Target;
        public readonly SolExpression ValueGetter;

        #region Overrides

        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            SolValue value = ValueGetter.Evaluate(context, parentVariables);
            try {
                Target.Set(value, context, parentVariables);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, ex.Message);
            }
            return value;
        }

        protected override string ToString_Impl()
        {
            return $"{Target} = {ValueGetter}";
        }

        #endregion

        #region Nested type: IndexedVariable

        /// <summary>
        ///     An indexed variable has an <see cref="IndexableGetter" /> and a <see cref="KeyGetter" />. The indexable getter must
        ///     return an <see cref="IValueIndexable" /> which will then be indexed by the result of the <see cref="KeyGetter" />.
        /// </summary>
        public class IndexedVariable : TargetRef
        {
            public IndexedVariable(SolExpression indexableGetter, SolExpression keyGetter)
            {
                IndexableGetter = indexableGetter;
                KeyGetter = keyGetter;
            }

            /// <summary>
            ///     The value that will be indexed. The return value must implement <see cref="IValueIndexable" />.
            /// </summary>
            public readonly SolExpression IndexableGetter;
            /// <summary>
            ///     The key by which the result of <see cref="IndexableGetter" /> will be indexed.
            /// </summary>
            public readonly SolExpression KeyGetter;

            #region Overrides

            /// <inheritdoc />
            /// <remarks> Evaluates the <see cref="IndexableGetter"/> first, then the <see cref="KeyGetter"/> </remarks>
            public override void Set(SolValue value, SolExecutionContext context, IVariables parentVariables)
            {
                SolValue indexableRaw = IndexableGetter.Evaluate(context, parentVariables);
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable == null) {
                    throw new SolVariableException("Cannot index the type \"" + indexableRaw.Type + "\".");
                }
                // <bubble>SolVariableException</bubble>
                indexable[KeyGetter.Evaluate(context, parentVariables)] = value;
            }

            public override string ToString()
            {
                return $"{IndexableGetter}[{KeyGetter}]";
            }

            #endregion
        }

        #endregion

        #region Nested type: NamedVariable

        /// <summary>
        ///     The <see cref="NamedVariable" /> will simply assign the value to the variable named <see cref="Name" />.
        /// </summary>
        public class NamedVariable : TargetRef
        {
            public NamedVariable(string name)
            {
                Name = name;
            }

            /// <summary>
            ///     The name of the variable the value will be assinged to.
            /// </summary>
            public readonly string Name;

            #region Overrides

            /// <inheritdoc />
            public override void Set(SolValue value, SolExecutionContext context, IVariables parentVariables)
            {
                // <bubble>SolVariableException</bubble>
                parentVariables.Assign(Name, value);
            }

            public override string ToString()
            {
                return Name;
            }

            #endregion
        }

        #endregion

        #region Nested type: TargetRef

        /// <summary>
        ///     The TargetRef class is used to provide an abstract way to set variables in a variable assignment statement.
        /// </summary>
        public abstract class TargetRef
        {
            /// <summary>
            ///     Sets the variable to the given value.
            /// </summary>
            /// <param name="value">The value to set the variable to.</param>
            /// <param name="context">The current context.</param>
            /// <param name="parentVariables">The parent variable context.</param>
            public abstract void Set(SolValue value, SolExecutionContext context, IVariables parentVariables);
        }

        #endregion
    }
}