using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Strings;
using SolScript.Compiler;
using SolScript.Exceptions;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;
using SolScript.Properties;

namespace SolScript.Interpreter.Statements
{
    /// <summary>
    ///     This statement declares a variable in the active chunk and optionally assigns an inital value to it.<br />
    ///     <a href="https://patrick-sachs.de/content/solscript/wiki/doku.php?id=spec:statement_variables">Wiki page</a>
    /// </summary>
    public class Statement_DeclareVar : SolStatement
    {
        /// <summary>
        ///     Creates a new statement.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="location">The source code location.</param>
        /// <param name="name">The variable name.</param>
        /// <param name="type">The variable type.</param>
        /// <param name="valueGetter">The optional inital value. (null if none)</param>
        public Statement_DeclareVar([NotNull] SolAssembly assembly, SourceLocation location, string name, SolType type, [CanBeNull] SolExpression valueGetter)
            : base(assembly, location)
        {
            Name = name;
            Type = type;
            ValueGetter = valueGetter;
        }

        /// <summary>
        ///     The variable name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The variable type.
        /// </summary>
        public readonly SolType Type;

        /// <summary>
        ///     The optional inital value. (null if none)
        /// </summary>
        [CanBeNull] public readonly SolExpression ValueGetter;
        
        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">An error occured while trying to assign declare the variable.</exception>
        public override SolValue Execute(SolExecutionContext context, IVariables parentVariables, out Terminators terminators)
        {
            context.CurrentLocation = Location;
            terminators = Terminators.None;
            try {
                parentVariables.Declare(Name, Type);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, "Failed to declare the variable \"" + Name + "\".", ex);
            }
            if (ValueGetter != null) {
                SolValue value = ValueGetter.Evaluate(context, parentVariables);
                try {
                    parentVariables.Assign(Name, value);
                } catch (SolVariableException ex) {
                    throw new SolRuntimeException(context, "Failed to assign the initial value to the variable \"" + Name + "\".", ex);
                }
                return value;
            }
            return SolNil.Instance;
        }

        /// <inheritdoc />
        protected override string ToString_Impl()
        {
            string middle = ValueGetter != null ? " = " + ValueGetter : string.Empty;
            return $"var {Name} : {Type}{middle}";
        }

        /// <inheritdoc />
        public override ValidationResult Validate(SolValidationContext context)
        {
            bool success = true;
            // If we are not in a chunk the variable declaration is not valid. + short circuit it.
            if (context.Chunks.Count == 0) {
                context.Errors.Add(new SolError(Location, ErrorId.None, CompilerResources.Err_VariableDeclarationNotInChunk.FormatWith(Name)));
                success = false;
            } else {
                SolValidationContext.Chunk chunkCtx = context.Chunks.Peek();
                // If another variable with the same name already exists the decl is not valid.
                if (context.HasChunkVariable(Name)) {
                    context.Errors.Add(new SolError(Location, CompilerResources.Err_DuplicateChunkVariable.FormatWith(Name)));
                    success = false;
                } else {
                    chunkCtx.AddVariable(Name, Type);
                }
                // If the variable getters fails or the types are not compatible the decl is not valid.
                if (ValueGetter != null) {
                    ValidationResult getterResult = ValueGetter.Validate(context);
                    if (!getterResult) {
                        success = false;
                    } else if (!Type.IsCompatible(Assembly, getterResult.Type)) {
                        context.Errors.Add(new SolError(Location, CompilerResources.Err_VariableInitializerTypeDoesNotMatchDeclaredType.FormatWith(Name, Type, getterResult.Type)));
                        success = false;
                    }
                }
            }
            return new ValidationResult(success, Type);
        }

        #endregion
    }
}