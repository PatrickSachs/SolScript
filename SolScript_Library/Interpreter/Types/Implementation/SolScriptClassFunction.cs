using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation {
    public class SolScriptClassFunction : SolFunction {
        private readonly SolType m_Type;

        public SolScriptClassFunction([NotNull] SolAssembly assembly, SourceLocation location, string classType, SolChunk chunk, SolType returnType, bool allowOptionalParams, [NotNull] params SolParameter[] parameters) : base(assembly, location, returnType, allowOptionalParams, parameters) {
            m_Type = new SolType(classType);
            Chunk = chunk;
        }

        public readonly SolChunk Chunk;

        #region Overrides
        
        public override SolValue Call(SolExecutionContext context, SolClass instance, params SolValue[] args) {
            if (!m_Type.IsCompatible(Assembly, instance.Type)) {
                throw new SolScriptException("invalid types");
            }
            ChunkVariables varContext = new ChunkVariables(Assembly) {
                Parent = instance.GlobalVariables
            };
            for (int i = 0; i < Parameters.Length; i++) {
                SolParameter parameter = Parameters[i];
                varContext.Declare(parameter.Name, parameter.Type);
                if (args.Length > i) {
                    // Enough arguments supplied
                    SolValue argument = args[i];
                    if (!parameter.Type.IsCompatible(Assembly, argument.Type)) {
                        throw new SolScriptException($"{context.CurrentLocation} : Invalid function call parameter types! Got '{argument.Type}', expected '{parameter.Type}'.");
                    }
                    varContext.Assign(parameter.Name, argument);
                } else {
                    // Not enough
                    if (!parameter.Type.CanBeNil) {
                        throw new SolScriptException($"{context.CurrentLocation} : Invalid function call parameter types! Got 'nil'(none passed), expected '{parameter.Type}'.");
                    }
                    varContext.Assign(parameter.Name, SolNil.Instance);
                }
            }
            // Additional arguments
            if (ParameterAllowOptional) {
                SolTable argsTable = new SolTable();
                for (int i = Parameters.Length; i < args.Length; i++) {
                    argsTable.Append(args[i]);
                }
                varContext.Put("args", new SolType("table"), argsTable);
            }
            SolValue ret = Chunk.ExecuteInTarget(context, varContext);
            if (!Return.IsCompatible(Assembly, ret.Type)) {
                throw new SolScriptInterpreterException(context, $"Invalid return value! Valid is '{Return}', got '{ret.Type}'!");
            }
            return ret;
        }

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            /*if (type == typeof (SolCSharpFunction.CSharpDelegate)) {
                SolCSharpFunction.CSharpDelegate csharpDel = args => this.Call(args, Owner.GlobalContext);
                return csharpDel;
            }*/
            throw new SolScriptMarshallingException("function", type);
        }

        /// <summary> Converts the value to a culture specfifc string. </summary>
        protected override string ToString_Impl([CanBeNull] SolExecutionContext context) {
            return "function" + Id + "<chunk#" + Chunk.Id + ">";
        }

        protected override int GetHashCode_Impl() {
            return 11 + Chunk.GetHashCode();
        }

        #endregion
    }
}