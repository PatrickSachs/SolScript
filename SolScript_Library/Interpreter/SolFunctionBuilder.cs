using System.Collections.Generic;
using System.Reflection;
using SolScript.Interpreter.Expressions;

namespace SolScript.Interpreter
{
    public sealed class SolAnnotationData
    {
        public SolAnnotationData(string name, params SolExpression[] arguments)
        {
            Name = name;
            Arguments = arguments;
        }

        public readonly SolExpression[] Arguments;
        public readonly string Name;
    }

    public sealed class SolFunctionBuilder
    {
        public SolFunctionBuilder(string name)
        {
            Name = name;
        }

        private readonly List<SolAnnotationData> m_Annotations = new List<SolAnnotationData>();

        private readonly List<SolParameter> m_Parameters = new List<SolParameter>();

        public IReadOnlyCollection<SolAnnotationData> Annotations => m_Annotations;

        public string Name { get; set; }
        public AccessModifier AccessModifier { get; set; }

        public SolSourceLocation Location { get; private set; }
        public bool IsNative { get; private set; }
        public MethodInfo NativeMethod { get; private set; }
        public SolChunk ScriptChunk { get; private set; }
        public SolParameter[] ScriptParameters => m_Parameters.ToArray();
        public bool ScriptAllowOptionalParameters { get; private set; }
        public SolType ScriptReturn { get; private set; }
        public ConstructorInfo NativeConstructor { get; private set; }

        public void AddAnnotation(SolAnnotationData annotation)
        {
            m_Annotations.Add(annotation);
        }

        public SolFunctionBuilder SetAccessModifier(AccessModifier modifier)
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
            ScriptReturn = default(SolType);
            ScriptAllowOptionalParameters = false;
            Location = SolSourceLocation.Native();
            return this;
        }

        public SolFunctionBuilder MakeNativeConstructor(ConstructorInfo constructor)
        {
            IsNative = true;
            NativeMethod = null;
            NativeConstructor = constructor;
            ScriptChunk = null;
            m_Parameters.Clear();
            ScriptReturn = default(SolType);
            ScriptAllowOptionalParameters = false;
            Location = SolSourceLocation.Native();
            return this;
        }

        public SolFunctionBuilder Return(SolType type)
        {
            ScriptReturn = type;
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