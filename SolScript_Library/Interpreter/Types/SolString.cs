using System;
using System.Text;
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

        /// <inheritdoc />
        public override string Type => TYPE;

        #region Overrides
        
        /// <inheritdoc />
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            if (type == typeof(string)) {
                return Value;
            }
            if (type == typeof(char)) {
                if (Value.Length != 1) {
                    throw new SolMarshallingException("string", typeof(char), "Can only convert strings with a length of one to a char! Size: " + Value.Length);
                }
                return Value[0];
            }
            if (type == typeof(char[])) {
                return Value.ToCharArray();
            }
            if (type == typeof(StringBuilder)) {
                return new StringBuilder(Value);
            }
            return base.ConvertTo(type);
        }

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 3 + Value.GetHashCode();
            }
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            if (other.Type != TYPE) {
                return false;
            }
            SolString otherStr = (SolString) other;
            return Value == otherStr.Value;
        }

        /// <inheritdoc />
        public override bool SmallerThan(SolExecutionContext context, SolValue other)
        {
            if (other.Type != TYPE) {
                return Bool_HelperThrowNotSupported(context, "compare(smaller)", TYPE, other.Type);
            }
            SolString otherStr = (SolString) other;
            return Value.Length < otherStr.Value.Length;
        }

        /// <inheritdoc />
        public override bool GreaterThan(SolExecutionContext context, SolValue other)
        {
            if (other.Type != TYPE) {
                return Bool_HelperThrowNotSupported(context, "compare(greater)", TYPE, other.Type);
            }
            SolString otherStr = (SolString) other;
            return Value.Length > otherStr.Value.Length;
        }

        /// <inheritdoc />
        public override SolNumber GetN(SolExecutionContext context)
        {
            return new SolNumber(Value.Length);
        }

        #endregion
    }
}