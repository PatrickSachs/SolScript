using System.Collections.Generic;
using SevenBiT.Inspector;
using SolScript.Interpreter.Expressions;

namespace SolScript.Interpreter
{
    public sealed class SolFieldBuilder {
        public SolFieldBuilder(string name, SolType type) {
            Name = name;
            Type = type;
        }

        //private readonly List<ClassDef.AnnotationDef> m_Annotations = new List<ClassDef.AnnotationDef>();
        public string Name { get; set; }
        public SolType Type { get; set; }
        public bool IsNativeField { get; private set; }
        public InspectorField NativeField { get; private set; }
        public SolExpression ScriptField { get; private set; }
        public AccessModifier AccessModifier { get; set; }

        private readonly List<SolAnnotationData> m_Annotations = new List<SolAnnotationData>();

        public IReadOnlyCollection<SolAnnotationData> Annotations => m_Annotations;

        public void AddAnnotation(SolAnnotationData annotation)
        {
            m_Annotations.Add(annotation);
        }

        public SolFieldBuilder SetAccessModifier(AccessModifier modifier)
        {
            AccessModifier = modifier;
            return this;
        }
        /*
        public SolFieldBuilder SetLocal(bool local) {
            if (local) {
                AccessModifier |= AccessModifier.Local;
            } else {
                AccessModifier &= ~AccessModifier.Local;
            }
            return this;
        }

        public SolFieldBuilder SetInternal(bool inter) {
            if (inter) {
                AccessModifier |= AccessModifier.Internal;
            } else {
                AccessModifier &= ~AccessModifier.Internal;
            }
            return this;
        }

        public SolFieldBuilder SetAbstract(bool abstr) {
            if (abstr) {
                AccessModifier |= Interpreter.AccessModifier.Abstract;
            } else {
                AccessModifier &= ~Interpreter.AccessModifier.Abstract;
            }
            return this;
        }*/

        /// <summary> Transforms this field into a native field.
        ///     <br/>
        ///     Warning: This will remove all annotations and custom field creators!
        ///     <br/>
        ///     Warning: Make sure the class instance this field is added to actually
        ///     proplery supports native fields of this type! </summary>
        /// <param name="field"> The native field. </param>
        public SolFieldBuilder MakeNativeField(InspectorField field) {
            IsNativeField = true;
            NativeField = field;
            ScriptField = null;
            //m_Annotations.Clear();
            return this;
        }

        public SolFieldBuilder MakeScriptField(SolExpression expression) {
            IsNativeField = false;
            ScriptField = expression;
            NativeField = null;
            return this;
        }

        /*public SolFieldBuilder AddAnnotation(ClassDef.AnnotationDef annotation) {
            m_Annotations.Add(annotation);
            return this;
        }

        public SolFieldBuilder AddAnnotations(params ClassDef.AnnotationDef[] annotations) {
            foreach (ClassDef.AnnotationDef annotation in annotations) {
                m_Annotations.Add(annotation);
            }
            return this;
        }

        public SolFieldBuilder AddAnnotations(IEnumerable<ClassDef.AnnotationDef> annotations) {
            foreach (ClassDef.AnnotationDef annotation in annotations) {
                m_Annotations.Add(annotation);
            }
            return this;
        }*/
    }
}