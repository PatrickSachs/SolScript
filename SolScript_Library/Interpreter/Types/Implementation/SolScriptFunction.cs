using System;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation {
    public class SolScriptFunction : SolFunction {
        public SolScriptFunction(SourceLocation location, VarContext context) : base(location, context) {
        }

        public SolChunk Chunk;
        //public SolCustomType DeclaringType;

        public override SolValue Call(SolValue[] args, SolExecutionContext context) {
            // The calling context for a function is pretty much irrelevant. The local 
            // vars of a function are determined by where the function has been declared, 
            // not from where it is being called.
            SolExecutionContext functionContext = new SolExecutionContext(context.Assembly);
            functionContext.VariableContext.ParentContext = ParentContext;
            for (int i = 0; i < Parameters.Length; i++)
            {
                SolParameter parameter = Parameters[i];
                if (args.Length > i)
                {
                    // Enough arguments supplied
                    SolValue argument = args[i];
                    if (!parameter.Type.IsCompatible(argument.Type))
                    {
                        throw new SolScriptException("Invalid function call parameter types! Got '" + argument.Type +
                                                     "', expected '" + parameter.Type +
                                                     (parameter.Type.CanBeNil ? "?" : "!") +
                                                     "'.");
                    }
                    functionContext.VariableContext.SetValue(parameter.Name, argument, parameter.Type, true);
                }
                else
                {
                    // Not enough
                    if (!parameter.Type.CanBeNil)
                    {
                        throw new SolScriptException(
                            "Invalid function call parameter types! Got 'nil'(none passed), expected '" +
                            parameter.Type + (parameter.Type.CanBeNil ? "?" : "!") + "'.");
                    }
                    // nil values are ignored anyway, so just do the check.
                }
            }
            // Additional arguments
            if (ParameterAllowOptional)
            {
                SolTable argsTable = new SolTable();
                for (int i = Parameters.Length; i < args.Length; i++)
                {
                    argsTable.Append(args[i]);
                }
                functionContext.VariableContext.SetValue("args", argsTable, new SolType("table", true), true);
            }
            SolValue ret = Chunk.Execute(functionContext, SolChunk.ContextMode.RunInParameter);
            if (!Return.IsCompatible(ret.Type)) {
                throw new SolScriptInterpreterException("Invalid return value! Valid is '" + Return + "', got '" +
                                                        ret.Type + "'!");
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
        protected override string ToString_Impl() {
            return "function<chunk#" + Chunk.Id + ">";
        }

        protected override int GetHashCode_Impl() {
            return 11 + Chunk.GetHashCode();
        }
    }
}