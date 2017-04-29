﻿using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Enumerables;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The function definition class is used by the Type Registry to save metadata about a function and to allow for a
    ///     two-step creation of functions.<br />
    ///     A function definition may or may not be for a class or global function.<br />
    /// </summary>
    public sealed class SolFunctionDefinition : SolAnnotateableDefinitionBase
    {
        internal SolFunctionDefinition(SolAssembly assembly, SourceLocation location) : base(assembly, location)
        {
            DeclaredAnnotations = ReadOnlyList<SolAnnotationDefinition>.FromValue(m_DeclaredAnnotationsList);
        }

        internal SolFunctionDefinition()
        {
            DeclaredAnnotations = ReadOnlyList<SolAnnotationDefinition>.FromValue(m_DeclaredAnnotationsList);
        }

        private readonly IList<SolAnnotationDefinition> m_DeclaredAnnotationsList = new PSUtility.Enumerables.List<SolAnnotationDefinition>();

        /// <inheritdoc />
        public override IReadOnlyList<SolAnnotationDefinition> DeclaredAnnotations { get; }

        /*/// <summary>
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
        /// <exception cref="SolMarshallingException">No matching SolType for the return type of the native builder/Invalid annotations.</exception>
        public SolFunctionDefinition(SolAssembly assembly, SolFunctionBuilder builder) : base(assembly, builder.Location)
        {
            Name = builder.Name;
            AccessModifier = builder.AccessModifier;
            MemberModifier = builder.MemberModifier;
            m_DeclaredAnnotations = InternalHelper.AnnotationsFromData(assembly, builder.Annotations);
            ReturnType = builder.ReturnType.Get(assembly);
            var parameters = new SolParameter[builder.Parameters.Count];
            for (int i = 0; i < parameters.Length; i++) {
                parameters[i] = builder.Parameters[i].Get(assembly);
            }
            if (builder.IsNative) {
                ParameterInfo = new SolParameterInfo.Native(parameters, builder.NativeMarshalTypes.ToArray(), builder.AllowOptionalParameters, builder.NativeSendContext);
                Chunk = builder.NativeMethod != null ? new SolChunkWrapper(builder.NativeMethod) : new SolChunkWrapper(builder.NativeConstructor);
            } else {
                ParameterInfo = new SolParameterInfo(parameters, builder.AllowOptionalParameters);
                Chunk = new SolChunkWrapper(builder.ScriptChunk);
            }
        }*/

        /// <summary>
        ///     The access modifier for this function which decide from where the function can be accessed.
        /// </summary>
        public SolAccessModifier AccessModifier { get; internal set; }

        /// <summary>
        ///     The wrapper around the actual code of the function. The function may either be a chunk declared in your code or a
        ///     native method/constructor.
        /// </summary>
        public SolChunkWrapper Chunk { get; internal set; }

        /// <summary>
        ///     The class this definition was defined in. If the function is a global function this value is null.
        /// </summary>
        [CanBeNull]
        public SolClassDefinition DefinedIn { get; internal set; }

        /// <summary>
        ///     The member modifier specifying the type of function.
        /// </summary>
        /// <seealso cref="SolMemberModifier" />
        public SolMemberModifier MemberModifier { get; internal set; }

        /// <summary>
        ///     The name of this function.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     Information about parameters of the function.
        /// </summary>
        public SolParameterInfo ParameterInfo { get; [UsedImplicitly] internal set; }

        /// <summary>
        ///     The return type of the function.
        /// </summary>
        public SolType ReturnType { get; internal set; }

        #region Overrides

        /// <inheritdoc />
        internal override void AddAnnotation(SolAnnotationDefinition annotation)
        {
            m_DeclaredAnnotationsList.Add(annotation);
        }

        #endregion
    }
}