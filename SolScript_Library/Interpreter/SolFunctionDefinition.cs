using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The function definition class is used by the Type Registry to save metadata about a function and to allow for a
    ///     two-step creation of functions.<br />
    ///     A function definion may or may not be for a class or global function.<br />
    ///     todo: investigate if builders + definitions can't be merged ...
    /// </summary>
    public class SolFunctionDefinition : ISourceLocateable
    {
        /// <summary>
        ///     Creates a new function definition for a function declared in a class.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups and register the definition to.</param>
        /// <param name="definedIn">The class the function belongs to.</param>
        /// <param name="builder">The function builder.</param>
        public SolFunctionDefinition(SolAssembly assembly, SolClassDefinition definedIn, SolFunctionBuilder builder) : this(assembly, builder)
        {
            DefinedIn = definedIn;
        }

        /// <summary>
        ///     Creates a new function definition for a global function.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups and register the definition to.</param>
        /// <param name="builder">The function builder.</param>
        public SolFunctionDefinition(SolAssembly assembly, SolFunctionBuilder builder)
        {
            Name = builder.Name;
            Location = builder.Location;
            Assembly = assembly;
            AccessModifier = builder.AccessModifier;
            if (builder.IsNative) {
                if (builder.NativeMethod != null) {
                    // Native Method
                    // todo: determine return type in library.
                    InternalHelper.GetMemberInfo(assembly, builder.NativeMethod, out ReturnType);
                    Chunk = new SolChunkWrapper(builder.NativeMethod);
                    ParameterInfo = InternalHelper.GetParameterInfo(assembly, builder.NativeMethod.GetParameters());
                    
                } else {
                    // Native Constructor
                    ReturnType = new SolType(SolNil.TYPE, true);
                    Chunk = new SolChunkWrapper(builder.NativeConstructor);
                    ParameterInfo = InternalHelper.GetParameterInfo(assembly, builder.NativeConstructor.GetParameters());
                }
            } else {
                // Script Chunk
                ReturnType = builder.ScriptReturn;
                ParameterInfo = new SolParameterInfo(builder.ScriptParameters, builder.ScriptAllowOptionalParameters);
                Chunk = new SolChunkWrapper(builder.ScriptChunk);
            }
        }

        /// <summary>
        /// The assembly this definition was defined in.
        /// </summary>
        public readonly SolAssembly Assembly;

        /// <summary>
        ///     The access modifier for this function which decide from where the function can be accessed.
        /// </summary>
        public readonly AccessModifier AccessModifier;

        /// <summary>
        ///     The wrapper around the actual code of the function. The function may either be a chunk declared in your code or a
        ///     native method/constructor.
        /// </summary>
        public readonly SolChunkWrapper Chunk;

        // todo: possibly class function definiton class?
        /// <summary>
        ///     The class this definition was defined in. If the function is a global function this value is null.
        /// </summary>
        [CanBeNull] public readonly SolClassDefinition DefinedIn;

        /// <summary>
        ///     The name of this function.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The return type of the function.
        /// </summary>
        public readonly SolType ReturnType;

        /// <summary>
        ///     Information about parameters of the function.
        /// </summary>
        public SolParameterInfo ParameterInfo;

        #region ISourceLocateable Members

        /// <summary>
        ///     Where in the code is this function located?
        /// </summary>
        public SolSourceLocation Location { get; }

        #endregion
    }
}