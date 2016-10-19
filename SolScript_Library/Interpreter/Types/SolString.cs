using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types {
    public class SolString : SolValue {
        public SolString(string value) {
            Value = value;
        }

        public const string STRING = "string";

        public static readonly SolString Empty = new SolString(string.Empty);

        public static readonly SolType MarshalFromCSharpType = new SolType(STRING, true);

        public readonly string Value;

        public override string Type { get; protected set; } = STRING;

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

        protected override string ToString_Impl() {
            return Value;
        }

        protected override int GetHashCode_Impl() {
            unchecked {
                return 3 + Value.GetHashCode();
            }
        }

        public override bool IsEqual(SolValue other) {
            if (other.Type != STRING) {
                return false;
            }
            SolString otherStr = (SolString) other;
            return Value == otherStr.Value;
        }

        public override bool SmallerThan(SolValue other) {
            if (other.Type != STRING) {
                return Bool_HelperThrowNotSupported("compare(smaller)", STRING, other.Type);
            }
            SolString otherStr = (SolString) other;
            return Value.Length < otherStr.Value.Length;
        }

        public override bool GreaterThan(SolValue other) {
            if (other.Type != STRING) {
                return Bool_HelperThrowNotSupported("compare(greater)", STRING, other.Type);
            }
            SolString otherStr = (SolString) other;
            return Value.Length > otherStr.Value.Length;
        }

        public override SolValue GetN() {
            return new SolNumber(Value.Length);
        }
    }
}