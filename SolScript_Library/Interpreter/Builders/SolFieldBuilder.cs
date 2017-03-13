using SolScript.Interpreter.Expressions;
using SolScript.Utility;

namespace SolScript.Interpreter.Builders
{
    /// <summary>
    ///     The <see cref="SolFieldBuilder" /> is used to create assembly independent, unvalidated fields which can be inserted
    ///     into an assembly to generate a proper <see cref="SolFieldDefinition" /> from.
    /// </summary>
    public sealed class SolFieldBuilder : SolBuilderBase, IAnnotateableBuilder, ISourceLocateable
    {
        private SolFieldBuilder(string name)
        {
            Name = name;
        }

        // The annotations on this field.
        private readonly List<SolAnnotationBuilder> m_Annotations = new List<SolAnnotationBuilder>();

        /// <summary>
        ///     The access modifier of this field.
        /// </summary>
        public SolAccessModifier AccessModifier { get; set; }

        /// <summary>
        ///     The unresolved type of this field.
        /// </summary>
        public SolTypeBuilder FieldType { get; private set; }

        /// <summary>
        ///     Is this field native?
        /// </summary>
        public bool IsNative { get; private set; }

        /// <summary>
        ///     The member modifier of this field.
        /// </summary>
        /// <remarks>This value is ignored, fields cannot have member modifiers.</remarks>
        public SolMemberModifier MemberModifier { get; set; }

        /// <summary>
        ///     The name of this field.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     The native backing field of this type. Only valid if <see cref="IsNative" /> is true.
        /// </summary>
        public FieldOrPropertyInfo NativeField { get; private set; }

        /// <summary>
        ///     The expression used to initialize the field. Only valid if <see cref="IsNative" /> is false.
        /// </summary>
        public SolExpression ScriptField { get; private set; }

        #region IAnnotateableBuilder Members

        /// <inheritdoc />
        public IReadOnlyList<SolAnnotationBuilder> Annotations => m_Annotations;

        /// <inheritdoc />
        public IAnnotateableBuilder AddAnnotation(SolAnnotationBuilder annotation)
        {
            m_Annotations.Add(annotation);
            return this;
        }

        /// <inheritdoc />
        public IAnnotateableBuilder ClearAnnotations()
        {
            m_Annotations.Clear();
            return this;
        }

        /// <inheritdoc />
        public IAnnotateableBuilder AddAnnotations(params SolAnnotationBuilder[] annotations)
        {
            m_Annotations.AddRange(annotations);
            return this;
        }

        #endregion

        #region ISourceLocateable Members

        /// <summary>
        ///     The location in source code.
        /// </summary>
        public SolSourceLocation Location { get; private set; }

        #endregion

        /// <inheritdoc cref="MemberModifier" />
        public SolFieldBuilder SetMemberModifier(SolMemberModifier modifier)
        {
            MemberModifier = modifier;
            return this;
        }

        /// <inheritdoc cref="AccessModifier" />
        public SolFieldBuilder SetAccessModifier(SolAccessModifier modifier)
        {
            AccessModifier = modifier;
            return this;
        }

        /// <inheritdoc cref="FieldType" />
        public SolFieldBuilder SetFieldType(SolTypeBuilder type)
        {
            FieldType = type;
            return this;
        }

        /// <summary>
        ///     Creates a new field builder for a native field.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="field"> The native field. </param>
        /// <returns>The field builder.</returns>
        public static SolFieldBuilder NewNativeField(string name, FieldOrPropertyInfo field)
        {
            SolFieldBuilder builder = new SolFieldBuilder(name);
            builder.IsNative = true;
            builder.NativeField = field;
            builder.ScriptField = null;
            builder.Location = SolSourceLocation.Native();
            return builder;
        }

        /// <summary>
        ///     Creates a new field builder for a script field.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="expression">The field initializer.</param>
        /// <returns>The field builder.</returns>
        public static SolFieldBuilder NewScriptField(string name, SolExpression expression)
        {
            SolFieldBuilder builder = new SolFieldBuilder(name);
            builder.IsNative = false;
            builder.NativeField = null;
            builder.ScriptField = expression;
            builder.Location = expression.Location;
            return builder;
        }
    }
}