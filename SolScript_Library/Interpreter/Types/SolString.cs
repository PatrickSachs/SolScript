using System;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types
{
    public sealed class SolString : SolValue
    {
        public SolString(string value)
        {
            Value = value;
        }

        public const string TYPE = "string";

        public static readonly SolString Empty = new SolString(string.Empty);
        
        public readonly string Value;

        public override string Type => TYPE;

        #region Overrides

        /// <summary>
        ///     Tries to convert the local value into a value of a C# type. May
        ///     return null.
        /// </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type)
        {
            if (type == typeof(SolValue) || type == typeof(SolString)) {
                return this;
            }
            if (type == typeof(string)) {
                return Value;
            }
            if (type == typeof(char)) {
                if (Value.Length != 1) {
                    throw new SolMarshallingException("string", typeof(char),
                        "The string has the wrong size! Length: " + Value.Length + ", Required: 1");
                }
                return Value[0];
            }
            throw new SolMarshallingException("string", type);
        }

        protected override string ToString_Impl([CanBeNull] SolExecutionContext context)
        {
            return Value;
        }

        public override int GetHashCode()
        {
            unchecked {
                return 3 + Value.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this)) {
                return true;
            }
            if (ReferenceEquals(obj, null)) {
                return false;
            }
            SolString otherSolString = obj as SolString;
            if (otherSolString == null) {
                return false;
            }
            return Value.Equals(otherSolString.Value);
        }

        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            if (other.Type != TYPE) {
                return false;
            }
            SolString otherStr = (SolString) other;
            return Value == otherStr.Value;
        }

        public override bool SmallerThan(SolExecutionContext context, SolValue other)
        {
            if (other.Type != TYPE) {
                return Bool_HelperThrowNotSupported(context, "compare(smaller)", TYPE, other.Type);
            }
            SolString otherStr = (SolString) other;
            return Value.Length < otherStr.Value.Length;
        }

        public override bool GreaterThan(SolExecutionContext context, SolValue other)
        {
            if (other.Type != TYPE) {
                return Bool_HelperThrowNotSupported(context, "compare(greater)", TYPE, other.Type);
            }
            SolString otherStr = (SolString) other;
            return Value.Length > otherStr.Value.Length;
        }

        public override SolNumber GetN(SolExecutionContext context)
        {
            return new SolNumber(Value.Length);
        }

        #endregion
    }
}