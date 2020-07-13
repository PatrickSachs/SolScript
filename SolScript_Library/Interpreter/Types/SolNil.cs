using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types {
    public class SolNil : SolValue
    {
        public const string TYPE = "nil";
        private SolNil() {
        }

        public static readonly SolNil Instance = new SolNil();

        // TODO: Nil as type constraint?
        public static readonly SolType MarshalFromCSharpType = new SolType("nil", true);

        public override string Type => TYPE;

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            if (type == typeof (SolValue) || type == typeof (SolNil)) {
                return this;
            }
            if (type.IsClass) {
                return null;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>)) {
                return null;
            }
            if (type == typeof (bool)) {
                return false;
            }
            if (type == typeof (int)) {
                return 0;
            }
            if (type == typeof (float)) {
                return 0f;
            }
            if (type == typeof (double)) {
                return 0d;
            }
            throw new SolScriptMarshallingException("nil", type);
        }

        protected override string ToString_Impl([CanBeNull]SolExecutionContext context) {
            return "nil";
        }

        public override bool IsEqual(SolExecutionContext context, SolValue other) {
            return other.Type == TYPE;
        }

        public override bool NotEqual(SolExecutionContext context, SolValue other) {
            return other.Type != TYPE;
        }

        protected override int GetHashCode_Impl() {
            return 0;
        }
        
        public override bool IsTrue(SolExecutionContext context) {
            return false;
        }
        
        public override bool IsFalse(SolExecutionContext context) {
            return true;
        }
    }
}