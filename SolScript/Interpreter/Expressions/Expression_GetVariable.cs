using JetBrains.Annotations;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter.Expressions
{
    /// <summary>
    ///     This expression is used to get a variable.
    /// </summary>
    public class Expression_GetVariable : SolExpression//, IWrittenInClass
    {
        /// <summary>
        ///     Used by the parser.
        /// </summary>
        public Expression_GetVariable() {}

        /// <summary>
        ///     Creates a new variable getter expression.
        /// </summary>
        /// <param name="variable">The variable to get.</param>
        public Expression_GetVariable(AVariable variable)
        {
            Variable = variable;
        }

        /// <summary>
        ///     The variable.
        /// </summary>
        public AVariable Variable { get; [UsedImplicitly] internal set; }

        /*#region IWrittenInClass Members
        /// <inheritdoc />
        public string WrittenInClass { get; [UsedImplicitly] internal set; }
        #endregion
        */

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">An errir occured while getting the variable.</exception>
        public override SolValue Evaluate(SolExecutionContext context, IVariables parentVariables)
        {
            context.CurrentLocation = Location;
            try {
                return Variable.Get(context, parentVariables);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Could not obtain the value of the desired variable.", ex);
            }
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            return Variable.ToString();
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            return Variable.Validate(context);
        }

        /// <inheritdoc />
        public override bool IsConstant => false;

        #endregion

        /*private SourceRef m_Source;

        /// <summary>
        /// Used by the parser.
        /// </summary>
        public Expression_GetVariable()
        {

        }

        /// <summary>
        ///     Creates the expression.
        /// </summary>
        /// <param name="source">The source used to actually get the variable.</param>
        /// <param name="writtenInClass">The class name this expression was written in.</param>
        /// <exception cref="InvalidOperationException">The variable source is already linked to another expression.</exception>
        public Expression_GetVariable(SourceRef source, string writtenInClass)
        {
            if (source.LinkedExpression != null && source.LinkedExpression != this) {
                throw new InvalidOperationException("The variable source is already linked to another expression - " + source.LinkedExpression);
            }
            Source = source;
            Source.LinkedExpression = this;
            WrittenInClass = writtenInClass;
        }

        /// <summary>
        ///     The source used to actually get the variable.
        /// </summary>
        /// <exception cref="ArgumentException" accessor="set">The variable source is already linked to another expression.</exception>
        public SourceRef Source {
            get { return m_Source; }
            [UsedImplicitly] internal set
            {
                if (value.LinkedExpression != null && value.LinkedExpression != this)
                {
                    throw new ArgumentException("The variable source is already linked to another expression - " + value.LinkedExpression, nameof(value));
                }
                m_Source = value;
            }
        }*/
        /*
        #region Nested type: IndexedVariable

        /// <summary>
        ///     An indexed variable has an <see cref="IndexableGetter" /> and a <see cref="KeyGetter" />. The indexable getter must
        ///     return an <see cref="IValueIndexable" /> which will then be indexed by the result of the <see cref="KeyGetter" />.
        /// </summary>
        public class IndexedVariable : SourceRef
        {
            /// <summary>
            ///     Creates a new indexed variable.
            /// </summary>
            /// <param name="indexableGetter">The expression used to get the value that should be indexed.</param>
            /// <param name="keyGetter">The expression used to get the value that should be used as indexer.</param>
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
            /// <exception cref="SolVariableException">
            ///     An error occured while retrieving this value. All other possible exceptions are
            ///     wrapped inside this exception. .
            /// </exception>
            public override SolValue Get(SolExecutionContext context, IVariables parentVariables)
            {
                SolValue indexableRaw = IndexableGetter.Evaluate(context, parentVariables);
                SolValue key = KeyGetter.Evaluate(context, parentVariables);
                SolClass solClass = indexableRaw as SolClass;
                if (solClass != null) {
                    SolString keyString = key as SolString;
                    if (keyString == null) {
                        throw new SolVariableException(KeyGetter.Location, $"Tried to index a class with a \"{key.Type}\" value.");
                    }
                    // 1 Inheritance could be found -> We can access locals! An inheritance can be found if the
                    //   get expression was declared inside the class.
                    // 2 Not found -> Only global access.
                    // Kind of funny how this little null coalescing operator handles the "deciding part" of access rights.
                    SolClass.Inheritance inheritance = LinkedExpression.WrittenInClass != null ? solClass.FindInheritance(LinkedExpression.WrittenInClass) : null;
                    SolValue value = inheritance?.GetVariables(SolAccessModifier.Local, SolVariableMode.All).Get(keyString.Value)
                                     ?? solClass.InheritanceChain.GetVariables(SolAccessModifier.Global, SolVariableMode.All).Get(keyString.Value);
                    return value;
                }
                IValueIndexable indexable = indexableRaw as IValueIndexable;
                if (indexable != null) {
                    SolValue value = indexable[key];
                    return value;
                }
                throw new SolVariableException(IndexableGetter.Location, "Tried to index a \"" + indexableRaw.Type + "\" value.");
            }

            /// <inheritdoc />
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
            /// <summary>
            ///     Creates a new named variable.
            /// </summary>
            /// <param name="name">The variable name.</param>
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
            /// <exception cref="SolVariableException">
            ///     An error occured while retrieving this value. All other possible exceptions are
            ///     wrapped inside this exception. .
            /// </exception>
            public override SolValue Get(SolExecutionContext context, IVariables parentVariables)
            {
                return parentVariables.Get(Name);
            }

            /// <inheritdoc />
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
            /// <summary>
            ///     The expression source variable source is assigned to(Updated automatically).
            /// </summary>
            public Expression_GetVariable LinkedExpression { get; internal set; }

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
        */
    }
}