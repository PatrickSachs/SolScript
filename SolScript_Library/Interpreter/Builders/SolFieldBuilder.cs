using System.Collections.Generic;
using SolScript.Interpreter;
using SolScript.Interpreter.Expressions;

namespace SolScript.Interpreter.Builders
{
    public sealed class SolFieldBuilder : SolBuilderBase, IAnnotateableBuilder
    {
        public SolFieldBuilder(string name)
        {
            Name = name;
        }

        private readonly List<SolAnnotationData> m_Annotations = new List<SolAnnotationData>();

        public string Name { get; set; }
        public SolType Type { get; set; }
        public bool IsNativeField { get; private set; }
        public FieldOrPropertyInfo NativeField { get; private set; }
        public SolExpression ScriptField { get; private set; }
        public SolAccessModifier AccessModifier { get; set; }
        public bool NativeReturnTypeHasBeenResolved { get; private set; }
        public SolSourceLocation Location { get; set; }

        public SolFieldBuilder AtLocation(SolSourceLocation location)
        {
            Location = location;
            return this;
        }

        #region IAnnotateableBuilder Members

        /// <inheritdoc />
        public IReadOnlyList<SolAnnotationData> Annotations => m_Annotations;

        /// <inheritdoc />
        public IAnnotateableBuilder AddAnnotation(SolAnnotationData annotation)
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
        public IAnnotateableBuilder AddAnnotations(params SolAnnotationData[] annotations)
        {
            m_Annotations.AddRange(annotations);
            return this;
        }
        
        #endregion

        public SolFieldBuilder SetAccessModifier(SolAccessModifier modifier)
        {
            AccessModifier = modifier;
            return this;
        }

        public SolFieldBuilder FieldNativeType(SolType type)
        {
            NativeReturnTypeHasBeenResolved = true;
            return FieldType(type);
        }

        public SolFieldBuilder FieldType(SolType type)
        {
            Type = type;
            return this;
        }

        /// <summary>
        ///     Transforms this field into a native field.
        ///     <br />
        ///     Warning: This will remove all annotations and custom field creators!
        ///     <br />
        ///     Warning: Make sure the class instance this field is added to actually
        ///     proplery supports native fields of this type!
        /// </summary>
        /// <param name="field"> The native field. </param>
        public SolFieldBuilder MakeNativeField(FieldOrPropertyInfo field)
        {
            IsNativeField = true;
            NativeField = field;
            ScriptField = null;
            Location = SolSourceLocation.Native();
            if (!NativeReturnTypeHasBeenResolved) {
                Type = default(SolType);
            }
            return this;
        }

        public SolFieldBuilder MakeScriptField(SolExpression expression)
        {
            IsNativeField = false;
            ScriptField = expression;
            NativeField = null;
            return this;
        }
    }
}