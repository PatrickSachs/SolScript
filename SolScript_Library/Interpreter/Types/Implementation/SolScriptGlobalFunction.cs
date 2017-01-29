﻿using System;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    public class SolScriptGlobalFunction : DefinedSolFunction
    {
        public SolScriptGlobalFunction(SolFunctionDefinition definition)
        {
            Definition = definition;
        }

        public override SolFunctionDefinition Definition { get; }

        #region Overrides

        public override object ConvertTo(Type type)
        {
            throw new NotImplementedException();
        }

        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<global>";
        }

        public override int GetHashCode()
        {
            unchecked {
                return 14 + (int) Id;
            }
        }

        public override bool Equals(object other)
        {
            return other == this;
        }

        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            // todo: internal access for global variables need to be figured out. (meaning, what does internal on globals even mean?)
            ChunkVariables varContext = new ChunkVariables(Assembly) {
                Parent = Assembly.LocalVariables
            };
            try {
                InsertParameters(varContext, args);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, ex.Message);
            }
            SolValue returnValue = Definition.Chunk.GetScriptChunk().ExecuteInTarget(context, varContext);
            return returnValue;
        }

        #endregion
    }
}