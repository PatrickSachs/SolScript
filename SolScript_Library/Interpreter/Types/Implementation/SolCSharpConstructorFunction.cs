using System;
using System.Reflection;
using Irony.Parsing;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types.Implementation {
    public class SolCSharpConstructorFunction : SolFunction {
        public SolCSharpConstructorFunction(ConstructorInfo constructor, SolCustomType typeInstance, int mixinId)
            : base(SourceLocation.Empty, new VarContext()) {
            Constructor = constructor;
            TypeInstance = typeInstance;
            MixinId = mixinId;
            Return = new SolType(typeInstance.Type, false);
        }
        
        public readonly ConstructorInfo Constructor;
        public readonly int MixinId;
        public readonly SolCustomType TypeInstance;

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            throw new NotSupportedException();
        }

        protected override string ToString_Impl() {
            return "function<clr#" +Constructor.Name+">";
        }

        protected override int GetHashCode_Impl() {
            unchecked {
                return 12 + MixinId + TypeInstance.GetHashCode();
            }
        }
        
        private bool m_DidCall;

        public override SolValue Call(SolValue[] args, SolExecutionContext context) {
            if (m_DidCall) {
                throw new SolScriptInterpreterException(Location + " : Tried to call a constructor in type " + TypeInstance.Type + " multiple times.");
            }
            m_DidCall = true;
            bool sendContext;
            // No marshall types are cached since the Ctor should only be called once anyway.
            var marshallTypes = SolCSharpFunction.GetParameterInfoTypes(Constructor.GetParameters(), out sendContext);
            var clrArgs = SolMarshal.Marshal(args, marshallTypes);
            object clrObject = Constructor.Invoke(clrArgs);
            TypeInstance.ClrObjects[MixinId] = clrObject;
            return TypeInstance;
        }
    }
}