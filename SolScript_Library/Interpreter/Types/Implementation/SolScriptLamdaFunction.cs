using System;
using System.Reflection;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    /// <summary>
    ///     Base class for all lambda <see cref="SolFunction" />s. Lamda functions can be described as anonymous functions and
    ///     thus are pretty much the purest form of a function. They do not require an actual definition and and can be created
    ///     at any given point without having to worry about anything.
    /// </summary>
    public abstract class SolLamdaFunction : SolFunction
    {
        // No third party primitives
        internal SolLamdaFunction(SolAssembly assembly, SolSourceLocation location, SolParameterInfo parameterInfo, SolType returnType)
        {
            Assembly = assembly;
            ParameterInfo = parameterInfo;
            ReturnType = returnType;
            Location = location;
        }

        /// <inheritdoc />
        public override SolAssembly Assembly { get; }

        /// <inheritdoc />
        public override SolParameterInfo ParameterInfo { get; }

        /// <inheritdoc />
        public override SolType ReturnType { get; }

        /// <inheritdoc />
        public override SolSourceLocation Location { get; }
    }

    /// <summary>
    ///     This class represents native lamda function.
    /// </summary>
    /// <seealso cref="SolLamdaFunction" />
    public sealed class SolNativeLamdaFunction : SolLamdaFunction
    {
        /// <summary>Creates a new native lamda functions for the given method.</summary>
        /// <param name="assembly">The assembly this function belongs to.</param>
        /// <param name="method">The native method representing this function.</param>
        /// <param name="instance">A reference to the object to invoke the method on.</param>
        /// <exception cref="SolMarshallingException">No matching SolType for a parameter type.</exception>
        public SolNativeLamdaFunction(SolAssembly assembly, MethodInfo method, DynamicReference instance)
            : base(assembly, SolSourceLocation.Native(),
                InternalHelper.GetParameterInfo(assembly, method.GetParameters()),
                InternalHelper.GetMemberReturnType(assembly, method))
        {
            m_Method = method;
            m_Instance = instance;
        }

        // The reference to the object to invoke the method on.
        private readonly DynamicReference m_Instance;
        private readonly MethodInfo m_Method;

        /// <inheritdoc />
        public new SolParameterInfo.Native ParameterInfo => (SolParameterInfo.Native) base.ParameterInfo;

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        /// <exception cref="InvalidOperationException">A critical internal error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            DynamicReference.GetState getState;
            object target = m_Instance.GetReference(out getState);
            if (getState != DynamicReference.GetState.Retrieved) {
                throw new InvalidOperationException($"Failed to retieve native object reference for native lamda function \"{m_Method.Name}\".");
            }
            object[] nativeArgs;
            try {
                nativeArgs = ParameterInfo.Marshal(context, args);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, $"Could not marshal arguments to native lamda function \"{m_Method.Name}\"", ex);
            }
            object nativeReturn = InternalHelper.SandboxInvokeMethod(context, m_Method, target, nativeArgs);
            SolValue solReturn;
            try {
                solReturn = SolMarshal.MarshalFromNative(Assembly, m_Method.ReturnType, nativeReturn);
            } catch (SolMarshallingException ex) {
                throw new SolRuntimeException(context, $"Could not marshal return value of type \"{nativeReturn?.GetType().Name ?? "null"}\" to SolScript.", ex);
            }
            return solReturn;
        }

        #endregion
    }

    /// <summary>
    ///     A lamda function. No real documentation yet since lamda functions are subject to change and to be expanded.
    /// </summary>
    public sealed class SolScriptLamdaFunction : SolLamdaFunction
    {
        public SolScriptLamdaFunction(SolAssembly assembly, SolSourceLocation location, SolParameterInfo parameterInfo,
            SolType returnType, SolChunk chunk, IVariables parentVariables)
            : base(assembly, location, parameterInfo, returnType)
        {
            m_Chunk = chunk;
            m_ParentVariables = parentVariables;
        }

        private readonly SolChunk m_Chunk;
        // todo: improve variable capturing for lamda functions. as of now it may captures EVERYTHING even if not required.
        private readonly IVariables m_ParentVariables;

        #region Overrides
        
        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return $"function#{Id}<lamda>";
        }

        /// <inheritdoc />
        /// <exception cref="SolRuntimeException">A runtime error occured.</exception>
        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            Variables variables = new Variables(Assembly) {Parent = m_ParentVariables};
            try {
                InsertParameters(variables, args);
            } catch (SolVariableException ex) {
                throw SolRuntimeException.InvalidFunctionCallParameters(context, ex);
            }
            // Functions pretty much eat the terminators since that's what the terminators are supposed to terminate down to.
            Terminators terminators;
            SolValue value = m_Chunk.Execute(context, variables, out terminators);
            return value;
        }

        #endregion
    }
}