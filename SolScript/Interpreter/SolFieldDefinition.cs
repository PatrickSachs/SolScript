using System.Collections.Generic;
using Irony.Parsing;
using JetBrains.Annotations;
using PSUtility.Enumerables;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     This definitions contains information about a field in SolScript.
    /// </summary>
    public sealed class SolFieldDefinition : SolAnnotateableDefinitionBase
    {
        /// <inheritdoc />
        public SolFieldDefinition(SolAssembly assembly, SourceLocation location) : base(assembly, location)
        {
        }

        internal SolFieldDefinition()
        {
        }

        private readonly PSList<SolAnnotationDefinition> m_DeclaredAnnotations = new PSList<SolAnnotationDefinition>();

        /// <inheritdoc />
        public override ReadOnlyList<SolAnnotationDefinition> DeclaredAnnotations => m_DeclaredAnnotations.AsReadOnly();

        /*/// <summary>
        ///     Creates a new field definition for a field located in a class.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups and register the definition to.</param>
        /// <param name="definedIn">The class the field was defined in.</param>
        /// <param name="builder">The field builder.</param>
        /// <exception cref="SolMarshallingException">No matching SolType for the native field type.</exception>
        public SolFieldDefinition(SolAssembly assembly, [CanBeNull] SolClassDefinition definedIn, SolFieldBuilder builder) : this(assembly, builder)
        {
            DefinedIn = definedIn;
        }

        /// <summary>
        ///     Creates a new field definition for a global field.
        /// </summary>
        /// <param name="assembly">The assembly to use for type lookups and register the definition to.</param>
        /// <param name="builder">The field builder.</param>
        /// <exception cref="SolMarshallingException">No matching SolType for the native field type/Invalid annotations.</exception>
        public SolFieldDefinition(SolAssembly assembly, SolFieldBuilder builder) : base(assembly, builder.Location)
        {
            Name = builder.Name;
            AccessModifier = builder.AccessModifier;
            Type = builder.FieldType.Get(assembly);
            DeclaredAnnotationsList = InternalHelper.AnnotationsFromData(assembly, builder.Annotations);
            Initializer = builder.IsNative ? new SolFieldInitializerWrapper(builder.NativeField) : new SolFieldInitializerWrapper(builder.ScriptField);
        }*/

        /// <summary>
        ///     The field's access modifier.
        /// </summary>
        public SolAccessModifier AccessModifier { get; internal set; }

        /// <summary>
        ///     The class this field was defined in. This is null for global fields.
        /// </summary>
        [CanBeNull]
        public SolClassDefinition DefinedIn { get; internal set; }

        /// <summary>
        ///     This class wraps the initializer of the field. Make sure to check the
        ///     <see cref="SolFieldInitializerWrapper.FieldType" /> before obtaining an actual reference.
        /// </summary>
        public SolFieldInitializerWrapper Initializer { get; internal set; }

        /// <summary>
        ///     The name of the field.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///     The data type of the field.
        /// </summary>
        public SolType Type { get; internal set; }

        #region Overrides

        /// <inheritdoc />
        internal override void AddAnnotation(SolAnnotationDefinition annotation)
        {
            m_DeclaredAnnotations.Add(annotation);
        }

        #endregion
    }
}