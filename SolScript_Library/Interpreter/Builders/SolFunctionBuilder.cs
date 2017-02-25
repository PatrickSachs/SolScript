using System.Collections.Generic;
using System.Reflection;
using SolScript.Interpreter.Expressions;

namespace SolScript.Interpreter.Builders
{
    public sealed class SolFunctionBuilder : SolBuilderBase, IAnnotateableBuilder
    {
        public SolFunctionBuilder(string name)
        {
            Name = name;
        }

        private readonly List<SolAnnotationData> m_Annotations = new List<SolAnnotationData>();
        private readonly List<SolParameter> m_Parameters = new List<SolParameter>();

        public string Name { get; set; }
        public SolAccessModifier AccessModifier { get; set; }

        public SolSourceLocation Location { get; private set; }
        public bool IsNative { get; private set; }
        public MethodInfo NativeMethod { get; private set; }
        public SolChunk ScriptChunk { get; private set; }
        public SolParameter[] ScriptParameters => m_Parameters.ToArray();
        public bool ScriptAllowOptionalParameters { get; private set; }
        public SolType ReturnType { get; private set; }
        public ConstructorInfo NativeConstructor { get; private set; }
        public bool NativeReturnTypeHasBeenResolved { get; private set; }

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

        public SolFunctionBuilder SetAccessModifier(SolAccessModifier modifier)
        {
            AccessModifier = modifier;
            return this;
        }

        public SolFunctionBuilder AtLocation(SolSourceLocation location)
        {
            Location = location;
            return this;
        }

        public SolFunctionBuilder MakeNativeFunction(MethodInfo method)
        {
            IsNative = true;
            NativeMethod = method;
            NativeConstructor = null;
            ScriptChunk = null;
            m_Parameters.Clear();
            ScriptAllowOptionalParameters = false;
            Location = SolSourceLocation.Native();
            if (!NativeReturnTypeHasBeenResolved) {
                ReturnType = default(SolType);
            }
            return this;
        }

        public SolFunctionBuilder MakeNativeConstructor(ConstructorInfo constructor)
        {
            IsNative = true;
            NativeMethod = null;
            NativeConstructor = constructor;
            ScriptChunk = null;
            m_Parameters.Clear();
            ScriptAllowOptionalParameters = false;
            Location = SolSourceLocation.Native();
            if (!NativeReturnTypeHasBeenResolved) {
                ReturnType = default(SolType);
            }
            return this;
        }

        public SolFunctionBuilder NativeReturns(SolType type)
        {
            NativeReturnTypeHasBeenResolved = true;
            return ScriptReturns(type);
        }

        public SolFunctionBuilder ScriptReturns(SolType type)
        {
            ReturnType = type;
            return this;
        }

        public SolFunctionBuilder OptionalParameters(bool allow)
        {
            ScriptAllowOptionalParameters = allow;
            return this;
        }

        public SolFunctionBuilder Chunk(SolChunk chunk)
        {
            ScriptChunk = chunk;
            return this;
        }

        public SolFunctionBuilder AddParameter(SolParameter parameter)
        {
            m_Parameters.Add(parameter);
            return this;
        }

        public SolFunctionBuilder AddParameters(IEnumerable<SolParameter> parameters)
        {
            m_Parameters.AddRange(parameters);
            return this;
        }

        public SolFunctionBuilder AddParameters(params SolParameter[] parameters)
        {
            return AddParameters((IEnumerable<SolParameter>) parameters);
        }

        public SolFunctionBuilder SetParameters(IEnumerable<SolParameter> parameters)
        {
            m_Parameters.Clear();
            m_Parameters.AddRange(parameters);
            return this;
        }

        public SolFunctionBuilder SetParameters(params SolParameter[] parameters)
        {
            return SetParameters((IEnumerable<SolParameter>) parameters);
        }
    }
}

namespace SolScript.Interpreter
{
    public sealed class SolAnnotationData
    {
        public SolAnnotationData(SolSourceLocation location, string name, params SolExpression[] arguments)
        {
            Name = name;
            Location = location;
            Arguments = arguments;
        }

        public readonly SolExpression[] Arguments;

        public readonly SolSourceLocation Location;
        public readonly string Name;
    }
}