using System;
using System.Collections.Generic;
using System.Reflection;
using SolScript.Interpreter.Expressions;
using SolScript.Utility;

namespace SolScript.Interpreter.Builders
{
    /// <summary>
    ///     The <see cref="SolFunctionBuilder" /> is used to create assembly independent, unvalidated functions which can be
    ///     inserted
    ///     into an assembly to generate a proper <see cref="SolFunctionDefinition" /> from.
    /// </summary>
    public sealed class SolFunctionBuilder : SolBuilderBase, IAnnotateableBuilder, ISourceLocateable
    {
        // Creates the builder and assigns the name.
        private SolFunctionBuilder(string name)
        {
            Name = name;
        }

        // The annotations on this builder.
        private readonly Utility.List<SolAnnotationData> m_Annotations = new Utility.List<SolAnnotationData>();
        // The function parameters.
        private readonly Utility.List<SolParameterBuilder> m_Parameters = new Utility.List<SolParameterBuilder>();
        // The list for native marshal types. Instance created by the static methods for native functions
        // to avoid the creation of unnecessary lists.
        private Utility.List<Type> l_native_marshal_types;

        /// <summary>
        ///     The name of this function?
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The access modifier of this function.
        /// </summary>
        public SolAccessModifier AccessModifier { get; private set; }

        /// <summary>
        ///     The member modifier of this function.
        /// </summary>
        public SolMemberModifier MemberModifier { get; private set; }

        /// <summary>
        ///     Is this a native function?
        /// </summary>
        public bool IsNative { get; private set; }

        /// <summary>
        ///     The native backing method. If <see cref="IsNative" /> is true this or <see cref="NativeConstructor" /> may be
        ///     valid.
        /// </summary>
        public MethodInfo NativeMethod { get; private set; }

        /// <summary>
        ///     The script chunk. Valid if <see cref="IsNative" /> is false.
        /// </summary>
        public SolChunk ScriptChunk { get; private set; }

        /// <summary>
        ///     The parameters of this function.
        /// </summary>
        public IReadOnlyList<SolParameterBuilder> Parameters => m_Parameters;

        /// <summary>
        ///     Are optional parmeters allowed?
        /// </summary>
        public bool AllowOptionalParameters { get; private set; }

        /// <summary>
        ///     The the current execution context be passed as first argument to a native method call?
        /// </summary>
        public bool NativeSendContext { get; private set; }

        /// <summary>
        ///     The return type of this function.
        /// </summary>
        public SolTypeBuilder ReturnType { get; private set; }

        /// <summary>
        ///     The native backing constructor. If <see cref="IsNative" /> is true this or <see cref="NativeMethod" /> may be
        ///     valid.
        /// </summary>
        public ConstructorInfo NativeConstructor { get; private set; }

        /// <summary>
        ///     The types SolValues will be marshalled to if calling the native method/ctor. Only valid if <see cref="IsNative" />
        ///     is true.
        /// </summary>
        public IReadOnlyList<Type> NativeMarshalTypes {
            get {
                if (!IsNative) {
                    return EmptyReadOnlyList<Type>.Value;
                }
                return l_native_marshal_types;
            }
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

        #region ISourceLocateable Members

        /// <inheritdoc />
        public SolSourceLocation Location { get; private set; }

        #endregion

        /// <inheritdoc cref="NativeMarshalTypes" />
        /// <exception cref="InvalidOperationException">The function is not a native function.</exception>
        /// <seealso cref="IsNative" />
        public SolFunctionBuilder AddNativeMarshalType(Type type)
        {
            if (!IsNative) {
                throw new InvalidOperationException("Only native functions can have native marshal types.");
            }
            l_native_marshal_types.Add(type);
            return this;
        }

        /// <inheritdoc cref="NativeMarshalTypes" />
        /// <exception cref="InvalidOperationException">The function is not a native function.</exception>
        /// <seealso cref="IsNative" />
        public SolFunctionBuilder ClearNativeMarshalTypes()
        {
            if (!IsNative) {
                throw new InvalidOperationException("Only native functions can have native marshal types.");
            }
            l_native_marshal_types.Clear();
            return this;
        }

        /// <inheritdoc cref="NativeMarshalTypes" />
        /// <exception cref="InvalidOperationException">The function is not a native function.</exception>
        /// <seealso cref="IsNative" />
        public SolFunctionBuilder AddNativeMarshalTypes(params Type[] types)
        {
            if (!IsNative) {
                throw new InvalidOperationException("Only native functions can have native marshal types.");
            }
            l_native_marshal_types.AddRange(types);
            return this;
        }

        /// <inheritdoc cref="NativeMarshalTypes" />
        /// <exception cref="InvalidOperationException">The function is not a native function.</exception>
        /// <seealso cref="IsNative" />
        public SolFunctionBuilder SetNativeMarshalTypes(params Type[] types)
        {
            if (!IsNative) {
                throw new InvalidOperationException("Only native functions can have native marshal types.");
            }
            l_native_marshal_types.Clear();
            l_native_marshal_types.AddRange(types);
            return this;
        }

        /// <inheritdoc cref="MemberModifier" />
        public SolFunctionBuilder SetMemberModifier(SolMemberModifier modifier)
        {
            MemberModifier = modifier;
            return this;
        }

        /// <inheritdoc cref="AccessModifier" />
        public SolFunctionBuilder SetAccessModifier(SolAccessModifier modifier)
        {
            AccessModifier = modifier;
            return this;
        }

        /// <summary>
        ///     Creates a new function builder for the given native method.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="method">The native method.</param>
        /// <returns>The builder.</returns>
        public static SolFunctionBuilder NewNativeFunction(string name, MethodInfo method)
        {
            SolFunctionBuilder builder = new SolFunctionBuilder(name);
            builder.IsNative = true;
            builder.NativeMethod = method;
            builder.NativeConstructor = null;
            builder.ScriptChunk = null;
            builder.Location = SolSourceLocation.Native();
            builder.l_native_marshal_types = new Utility.List<Type>();
            return builder;
        }

        /// <summary>
        ///     Creates a new function builder for the given native constructor.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="constructor">The native constructor.</param>
        /// <returns>The builder.</returns>
        public static SolFunctionBuilder NewNativeConstructor(string name, ConstructorInfo constructor)
        {
            SolFunctionBuilder builder = new SolFunctionBuilder(name);
            builder.IsNative = true;
            builder.NativeMethod = null;
            builder.NativeConstructor = constructor;
            builder.ScriptChunk = null;
            builder.Location = SolSourceLocation.Native();
            builder.l_native_marshal_types = new Utility.List<Type>();
            return builder;
        }

        /// <inheritdoc cref="NewScriptFunction(string,SolChunk,SolSourceLocation)" />
        /// <param name="name">The function name.</param>
        /// <param name="chunk">The chunk.</param>
        public static SolFunctionBuilder NewScriptFunction(string name, SolChunk chunk)
        {
            return NewScriptFunction(name, chunk, chunk.Location);
        }

        /// <summary>
        ///     Creates a new function builder for the given script chunk.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="chunk">The chunk.</param>
        /// <param name="location">The source location this function is at.</param>
        /// <returns>The builder.</returns>
        public static SolFunctionBuilder NewScriptFunction(string name, SolChunk chunk, SolSourceLocation location)
        {
            SolFunctionBuilder builder = new SolFunctionBuilder(name);
            builder.IsNative = false;
            builder.NativeMethod = null;
            builder.NativeConstructor = null;
            builder.ScriptChunk = chunk;
            builder.Location = location;
            return builder;
        }

        /// <inheritdoc cref="ReturnType" />
        public SolFunctionBuilder SetReturnType(SolTypeBuilder type)
        {
            ReturnType = type;
            return this;
        }

        /// <inheritdoc cref="AllowOptionalParameters" />
        public SolFunctionBuilder SetAllowOptionalParameters(bool allow)
        {
            AllowOptionalParameters = allow;
            return this;
        }

        /// <inheritdoc cref="NativeSendContext" />
        /// <exception cref="InvalidOperationException">The function is not native.</exception>
        /// <seealso cref="IsNative" />
        public SolFunctionBuilder SetNativeSendContext(bool send)
        {
            if (!IsNative) {
                throw new InvalidOperationException("Only native functions can be sent the current execution context.");
            }
            NativeSendContext = send;
            return this;
        }

        /// <summary>
        ///     Adds a new parameter to this function.
        /// </summary>
        /// <param name="parameter">The parameter builder.</param>
        /// <returns>The builder itself.</returns>
        public SolFunctionBuilder AddParameter(SolParameterBuilder parameter)
        {
            m_Parameters.Add(parameter);
            return this;
        }

        /// <summary>
        ///     Adds new parameters to this function.
        /// </summary>
        /// <param name="parameters">The parameter builders.</param>
        /// <returns>The builder itself.</returns>
        public SolFunctionBuilder AddParameters(IEnumerable<SolParameterBuilder> parameters)
        {
            m_Parameters.AddRange(parameters);
            return this;
        }

        /// <summary>
        ///     Adds new parameters to this function.
        /// </summary>
        /// <param name="parameters">The parameter builders.</param>
        /// <returns>The builder itself.</returns>
        public SolFunctionBuilder AddParameters(params SolParameterBuilder[] parameters)
        {
            return AddParameters((IEnumerable<SolParameterBuilder>) parameters);
        }

        /// <summary>
        ///     Sets the parameters of this function, overriding the old ones.
        /// </summary>
        /// <param name="parameters">The parameter builders.</param>
        /// <returns>The builder itself.</returns>
        public SolFunctionBuilder SetParameters(IEnumerable<SolParameterBuilder> parameters)
        {
            m_Parameters.Clear();
            m_Parameters.AddRange(parameters);
            return this;
        }

        /// <summary>
        ///     Sets the parameters of this function, overriding the old ones.
        /// </summary>
        /// <param name="parameters">The parameter builders.</param>
        /// <returns>The builder itself.</returns>
        public SolFunctionBuilder SetParameters(params SolParameterBuilder[] parameters)
        {
            return SetParameters((IEnumerable<SolParameterBuilder>) parameters);
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