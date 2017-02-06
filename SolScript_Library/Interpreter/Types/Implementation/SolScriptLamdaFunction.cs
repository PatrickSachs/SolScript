using System;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    public class SolScriptLamdaFunction : SolFunction
    {
        public SolScriptLamdaFunction(SolAssembly assembly, SolSourceLocation location, SolChunk chunk, IVariables parentVariables, SolType returnType, SolParameterInfo parameterInfo)
        {
            m_Chunk = chunk;
            m_ParentVariables = parentVariables;
            Assembly = assembly;
            Location = location;
            ReturnType = returnType;
            ParameterInfo = parameterInfo;
        }

        private readonly SolChunk m_Chunk;
        // todo: improve variable mcapturing for lamda functions. as of now it may capture entire class hierachies (but maybe that's desired?)
        private readonly IVariables m_ParentVariables;

        public override SolAssembly Assembly { get; }
        public override SolType ReturnType { get; }
        public override SolSourceLocation Location { get; }

        public override SolParameterInfo ParameterInfo { get; }

        #region Overrides

        public override object ConvertTo(Type type)
        {
            throw new NotImplementedException();
        }

        protected override string ToString_Impl(SolExecutionContext context)
        {
            return $"function#{Id}<lamda>";
        }

        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            Variables variables = new Variables(Assembly) {Parent = m_ParentVariables};
            try {
                InsertParameters(variables, args);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, ex.Message);
            }
            // Functions pretty much eat the terminators since that's what the terminators are supposed to terminate down to.
            Terminators terminators;
            SolValue value = m_Chunk.Execute(context, variables, out terminators);
            return value;
        }

        #endregion
    }
}