using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;
using SolScript.Interpreter.Types.Implementation;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Expressions
{
    public class Expression_GetVariable : SolExpression, IWrittenInClass
    {
        public Expression_GetVariable(SolAssembly assembly, SolSourceLocation location, SourceRef source, string writtenInClass) : base(assembly, location)
        {
            Source = source;
            Source.LinkedExpression = this;
            WrittenInClass = writtenInClass;
        }

        public readonly SourceRef Source;

        #region Overrides

        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            try {
                return Source.Get(context, parentVariables);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, ex.Message);
            }
        }

        protected override string ToString_Impl()
        {
            return Source.ToString();
        }

        public string WrittenInClass { get; }

        #endregion

        #region Nested type: IndexedVariable

        /// <summary>
        ///     An indexed variable has an <see cref="IndexableGetter" /> and a <see cref="KeyGetter" />. The indexable getter must
        ///     return an <see cref="IValueIndexable" /> which will then be indexed by the result of the <see cref="KeyGetter" />.
        /// </summary>
        public class IndexedVariable : SourceRef
        {
            public IndexedVariable(SolExpression indexableGetter, SolExpression keyGetter)
            {
                KeyGetter = keyGetter;
                IndexableGetter = indexableGetter;
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
            public override SolValue Get(SolExecutionContext context, IVariables parentVariables)
            {
                SolValue indexableRaw = IndexableGetter.Evaluate(context, parentVariables);
                SolValue key = KeyGetter.Evaluate(context, parentVariables);
                SolClass solClass = indexableRaw as SolClass;
                if (solClass != null) {
                    SolString keyString = key as SolString;
                    if (keyString == null) {
                        throw new SolVariableException($"Tried to index a class by a \"{key.Type}\" value.");
                    }
                    SolClass.Inheritance inheritance = LinkedExpression.WrittenInClass != null ? solClass.FindInheritance(LinkedExpression.WrittenInClass) : null;
                    // <bubble>SolVariableException</bubble>
                    return inheritance?.Variables.Get(keyString.Value) ?? solClass.GlobalVariables.Get(keyString.Value);
                }
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable != null)
                {
                    // <bubble>SolVariableException</bubble>
                    SolValue value = indexable[key];
                    return value;
                }
                throw new SolVariableException("Tried to index a \"" + indexableRaw.Type + "\" value.");
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
        ///     A named variable is directly retrieved from the parent variables using the given <see cref="Name" />.
        /// </summary>
        public class NamedVariable : SourceRef
        {
            public NamedVariable(string name)
            {
                Name = name;
            }


            /// <summary>
            ///     The name of this variable.
            /// </summary>
            public readonly string Name;

            #region Overrides

            /// <inheritdoc />
            public override SolValue Get(SolExecutionContext context, IVariables parentVariables)
            {
                // <bubble>SolVariableException</bubble>
                return parentVariables.Get(Name);
            }

            public override string ToString()
            {
                return Name;
            }

            #endregion
        }

        #endregion

        #region Nested type: SourceRef

        /// <summary>
        ///     A SourceRef class is used to express how a variable will be obtained.
        /// </summary>
        public abstract class SourceRef
        {
            public Expression_GetVariable LinkedExpression;

            /// <summary>
            ///     Gets the value.
            /// </summary>
            /// <param name="context">The context to use.</param>
            /// <param name="parentVariables">The parented variables.</param>
            /// <returns>The value.</returns>
            /// <exception cref="SolVariableException">
            ///     An error occured while retrieving this value. All other possible exceptions are
            ///     wrapped inside this exception.
            /// </exception>
            public abstract SolValue Get(SolExecutionContext context, IVariables parentVariables);
        }

        #endregion
    }
}