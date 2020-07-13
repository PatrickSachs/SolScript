using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types {
    public class SolString : SolValue {
        public SolString(string value) {
            Value = value;
        }

        public const string TYPE = "string";

        public static readonly SolString Empty = new SolString(string.Empty);

        public static readonly SolType MarshalFromCSharpType = new SolType(TYPE, true);

        public readonly string Value;

        public override string Type => TYPE;

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            if (type == typeof (SolValue) || type == typeof (SolString)) {
                return this;
            }
            if (type == typeof (string)) {
                return Value;
            }
            if (type == typeof (char)) {
                if (Value.Length != 1) {
                    throw new SolScriptMarshallingException("string", typeof (char),
                        "The string has the wrong size! Length: " + Value.Length + ", Required: 1");
                }
                return Value[0];
            }
            throw new SolScriptMarshallingException("string", type);
        }

        protected override string ToString_Impl([CanBeNull]SolExecutionContext context) {
            return Value;
        }

        protected override int GetHashCode_Impl() {
            unchecked {
                return 3 + Value.GetHashCode();
            }
        }

        public override bool IsEqual(SolExecutionContext context, SolValue other) {
            if (other.Type != TYPE) {
                return false;
            }
            SolString otherStr = (SolString) other;
            return Value == otherStr.Value;
        }

        public override bool SmallerThan(SolExecutionContext context, SolValue other) {
            if (other.Type != TYPE) {
                return Bool_HelperThrowNotSupported("compare(smaller)", TYPE, other.Type);
            }
            SolString otherStr = (SolString) other;
            return Value.Length < otherStr.Value.Length;
        }

        public override bool GreaterThan(SolExecutionContext context, SolValue other) {
            if (other.Type != TYPE) {
                return Bool_HelperThrowNotSupported("compare(greater)", TYPE, other.Type);
            }
            SolString otherStr = (SolString) other;
            return Value.Length > otherStr.Value.Length;
        }

        public override SolValue GetN(SolExecutionContext context) {
            return new SolNumber(Value.Length);
        }
    }
}