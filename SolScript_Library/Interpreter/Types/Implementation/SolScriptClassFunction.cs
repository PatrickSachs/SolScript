using System;
using System.Windows.Forms.VisualStyles;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation
{
    public class SolScriptClassFunction : SolClassFunction
    {
        public SolScriptClassFunction(SolClass inClass, SolFunctionDefinition definition)
        {
            m_HoldingClass = inClass;
            Definition = definition;
        }

        private readonly SolClass m_HoldingClass;

        #region Overrides

        protected override SolValue Call_Impl(SolExecutionContext context, params SolValue[] args)
        {
            SolClass.Inheritance inheritance = m_HoldingClass.FindInheritance(Definition.DefinedIn).NotNull();
            ChunkVariables varContext = new ChunkVariables(Assembly) {
                Parent = inheritance.Variables // todo: orginally this was parented to the internal variables? was this a mistake or does it mave a reason?
            };
            try {
                InsertParameters(varContext, args);
            } catch (SolVariableException ex) {
                throw new SolRuntimeException(context, ex.Message);
            }
            SolValue returnValue = Definition.Chunk.GetScriptChunk().ExecuteInTarget(context, varContext);
            return returnValue;
        }

        /// <summary>
        ///     Tries to convert the local value into a value of a C# type. May
        ///     return null.
        /// </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            /*if (type == typeof (SolCSharpFunction.CSharpDelegate)) {
                SolCSharpFunction.CSharpDelegate csharpDel = args => this.Call(args, Owner.GlobalContext);
                return csharpDel;
            }*/
            throw new SolMarshallingException("function", type);
        }

        /// <summary> Converts the value to a culture specfifc string. </summary>
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "function#" + Id + "<class#" + Definition.DefinedIn.Type + ">";
        }

        public override int GetHashCode()
        {
            unchecked {
                return 11 + (int)Id;
            }
        }

        public override bool Equals(object other)
        {
            return other == this;
        }

        public override SolClassDefinition GetDefiningClass()
        {
            return Definition.DefinedIn;
        }

        public override SolAssembly Assembly {
            get {
                if (Definition.DefinedIn != null) return Definition.DefinedIn.Assembly;
                throw new InvalidOperationException("Could not get the defining class of a class function.");
            }
        }

        public override SolFunctionDefinition Definition { get; }

        #endregion
    }
}