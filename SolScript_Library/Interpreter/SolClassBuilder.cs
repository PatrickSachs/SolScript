using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using SevenBiT.Inspector;
using SolScript.Interpreter.Expressions;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter {
    /// <summary> A class builder is used to define classes before they are processed
    ///     by the TypeRegistry. </summary>
    public sealed class SolClassBuilder {
        /// <summary> Creates a new class builder. </summary>
        /// <param name="name"> The type name of the class you wish to create. </param>
        /// <param name="typeMode"> Which type of class is the created class? </param>
        public SolClassBuilder(string name, SolTypeMode typeMode) {
            Name = name;
            TypeMode = typeMode;
        }

        private readonly Dictionary<string, SolFieldBuilder> m_Fields = new Dictionary<string, SolFieldBuilder>();
        private readonly Dictionary<string, SolFunctionBuilder> m_Functions = new Dictionary<string, SolFunctionBuilder>();
        public string BaseClass { get; set; }

        /// <summary> The native class this SolClass will marshal to and from. </summary>
        public Type NativeType { get; set; }

        /// <summary> The name of the class. </summary>
        public string Name { get; set; }

        /// <summary> Which type of class is the created class? </summary>
        public SolTypeMode TypeMode { get; set; }

        public IReadOnlyCollection<SolFieldBuilder> Fields => m_Fields.Values;
        public IReadOnlyCollection<SolFunctionBuilder> Functions => m_Functions.Values;

        public SolClassBuilder SetNativeType(Type type) {
            NativeType = type;
            return this;
        }

        /// <summary> Adds a field with the given name and field definition to the builder.
        ///     Overrides old entries. </summary>
        /// <param name="field"> The field definition itself. </param>
        /// <param name="overwrite"> Should existing fields be overwritten? </param>
        /// <exception cref="ArgumentException"> A field with this name already exists and
        ///     and <paramref name="overwrite"/> is false. </exception>
        public SolClassBuilder AddField(SolFieldBuilder field, [UsedImplicitly] bool overwrite = false) {
            if (!overwrite && m_Fields.ContainsKey(field.Name)) {
                throw new ArgumentException("The field \"" + field.Name + "\" already exists, and overwrite it set to " + false + "!", nameof(field));
            }
            m_Fields[field.Name] = field;
            return this;
        }

        public SolClassBuilder AddFunction(SolFunctionBuilder function, [UsedImplicitly] bool overwrite = false) {
            if (!overwrite && m_Fields.ContainsKey(function.Name)) {
                throw new ArgumentException("The function \"" + function.Name + "\" already exists, and overwrite it set to " + false + "!", nameof(function));
            }
            m_Functions[function.Name] = function;
            return this;
        }

        public SolClassBuilder Extends(string baseClass) {
            BaseClass = baseClass;
            return this;
        }
    }

    public sealed class SolFunctionBuilder {
        public SolFunctionBuilder(string name) {
            Name = name;
        }

        public string Name { get; set; }
        public AccessModifiers Modifiers { get; set; }

        public bool IsNativeFunction { get; private set; }
        public MethodInfo NativeMethod { get; private set; }
        public SolFunction ScriptFunction { get; private set; }
        public ConstructorInfo NativeConstructor { get; private set; }

        public SolFunctionBuilder SetLocal(bool local) {
            if (local) {
                Modifiers |= AccessModifiers.Local;
            } else {
                Modifiers &= ~AccessModifiers.Local;
            }
            return this;
        }

        public SolFunctionBuilder SetInternal(bool inter) {
            if (inter) {
                Modifiers |= AccessModifiers.Internal;
            } else {
                Modifiers &= ~AccessModifiers.Internal;
            }
            return this;
        }

        public SolFunctionBuilder SetAbstract(bool abstr) {
            if (abstr) {
                Modifiers |= AccessModifiers.Abstract;
            } else {
                Modifiers &= ~AccessModifiers.Abstract;
            }
            return this;
        }

        public SolFunctionBuilder MakeNativeFunction(MethodInfo method) {
            IsNativeFunction = true;
            NativeMethod = method;
            NativeConstructor = null;
            ScriptFunction = null;
            return this;
        }

        public SolFunctionBuilder MakeNativeConstructor(ConstructorInfo constructor) {
            IsNativeFunction = true;
            NativeMethod = null;
            NativeConstructor = constructor;
            ScriptFunction = null;
            return this;
        }

        public SolFunctionBuilder MakeScriptFunction(SolFunction function) {
            IsNativeFunction = false;
            NativeMethod = null;
            NativeConstructor = null;
            ScriptFunction = function;
            return this;
        }
    }

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
        public AccessModifiers Modifiers { get; set; }

        public SolFieldBuilder SetLocal(bool local) {
            if (local) {
                Modifiers |= AccessModifiers.Local;
            } else {
                Modifiers &= ~AccessModifiers.Local;
            }
            return this;
        }

        public SolFieldBuilder SetInternal(bool inter) {
            if (inter) {
                Modifiers |= AccessModifiers.Internal;
            } else {
                Modifiers &= ~AccessModifiers.Internal;
            }
            return this;
        }

        public SolFieldBuilder SetAbstract(bool abstr) {
            if (abstr) {
                Modifiers |= AccessModifiers.Abstract;
            } else {
                Modifiers &= ~AccessModifiers.Abstract;
            }
            return this;
        }

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