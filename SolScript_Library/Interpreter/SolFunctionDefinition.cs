using System.Collections.Generic;
using JetBrains.Annotations;
using SolScript.Interpreter.Builders;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The function definition class is used by the Type Registry to save metadata about a function and to allow for a
    ///     two-step creation of functions.<br />
    ///     A function definion may or may not be for a class or global function.<br />
    /// </summary>
    public sealed class SolFunctionDefinition : SolAnnotateableDefinitionBase
    {
        /// <summary>
        ///     Creates a new function definition for a function declared in a class.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups and register the definition to.</param>
        /// <param name="definedIn">The class the function belongs to.</param>
        /// <param name="builder">The function builder.</param>
        /// <exception cref="SolMarshallingException">No matching SolType for the return type of the native builder.</exception>
        public SolFunctionDefinition(SolAssembly assembly, SolClassDefinition definedIn, SolFunctionBuilder builder) : this(assembly, builder)
        {
            DefinedIn = definedIn;
        }

        /// <summary>
        ///     Creates a new function definition for a global function.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups and register the definition to.</param>
        /// <param name="builder">The function builder.</param>
        /// <exception cref="SolMarshallingException">No matching SolType for the return type of the native builder.</exception>
        public SolFunctionDefinition(SolAssembly assembly, SolFunctionBuilder builder) : base(assembly, builder.Location)
        {
            Name = builder.Name;
            AccessModifier = builder.AccessModifier;
            AnnotationsFromData(builder.Annotations);
            if (builder.IsNative) {
                if (builder.NativeMethod != null) {
                    // Native Method
                    if (builder.NativeReturnTypeHasBeenResolved) {
                        // If the native type has already been resolved, just set it.
                        // Note: I am fully aware that the type should only need to be resolved once and caching
                        // it is thus theoretically useless. However, there are some ways to "fake-resolve" the
                        // return type earlier(e.g. ToString() would resolve to string?, but a fake-resolver changes
                        // this to string! in order to be compatible with SolScripts __to_string() meta-method).
                        ReturnType = builder.ReturnType;
                    } else {
                        // The return type cannot be fully determined in the library since other libraries may
                        // be required in order for the return type to be resolved.
                        ReturnType = InternalHelper.GetMemberReturnType(assembly, builder.NativeMethod);
                        builder.NativeReturns(ReturnType);
                    }
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
                ReturnType = builder.ReturnType;
                ParameterInfo = new SolParameterInfo(builder.ScriptParameters, builder.ScriptAllowOptionalParameters);
                Chunk = new SolChunkWrapper(builder.ScriptChunk);
            }
        }

        /// <summary>
        ///     The access modifier for this function which decide from where the function can be accessed.
        /// </summary>
        public readonly SolAccessModifier AccessModifier;

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
        public readonly SolParameterInfo ParameterInfo;

        /// <inheritdoc />
        public override IReadOnlyList<SolAnnotationDefinition> Annotations { get; protected set; }
    }
}