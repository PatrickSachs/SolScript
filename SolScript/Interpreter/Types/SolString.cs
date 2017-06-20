using System;
using System.Collections.Generic;
using System.Text;
using SolScript.Exceptions;
using SolScript.Interpreter.Types.Marshal;

namespace SolScript.Interpreter.Types
{
    /// <summary>
    ///     The <see cref="SolString" /> is used to represent well... a string. As opposed to may other languages a string is
    ///     an actual primitive in SolScript. There is not character type. Characters are either represented by a string with a
    ///     length of one or a number.
    /// </summary>
    public sealed class SolString : SolValue
    {
        static SolString()
        {
            // Intern some often used values.
            new SolString(" ").Intern();
        }

        // Private constructor to support interning.
        private SolString(string value)
        {
            Value = value;
        }

        /// <summary>
        ///     The type name is "string".
        /// </summary>
        public const string TYPE = "string";

        // All interned strings.
        private static readonly Dictionary<string, SolString> s_Interned = new Dictionary<string, SolString>();

        /// <inheritdoc cref="string.Empty" />
        public static readonly SolString Empty = new SolString(string.Empty).Intern();

        /// <summary>
        ///     The current value of this string.
        /// </summary>
        public readonly string Value;

        /// <inheritdoc />
        public override string Type => TYPE;

        #region Overrides

        /// <inheritdoc />
        public override bool IsReferenceEqual(SolExecutionContext context, SolValue other)
        {
            return IsEqual(context, other);
        }

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
            if (type.IsEnum) {
                NativeEnumMarshaller.EnumData data = NativeEnumMarshaller.GetEnumData(type);
                Enum e;
                // todo: support flags
                if (data.QueryName(Value, out e)) {
                    return e;
                }
                throw new SolMarshallingException(TYPE, type, "Cannot find matching enum value: " + Value);
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

        /// <summary>
        ///     Gets the <see cref="SolString" /> of the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created string.</returns>
        public static SolString ValueOf(string value)
        {
            SolString str;
            if (s_Interned.TryGetValue(value, out str)) {
                return str;
            }
            return new SolString(value);
        }

        /// <summary>
        ///     Interns the string. Interned strings only exist once in memory and thus safe memory if they are expected to exist
        ///     very often within your application.
        /// </summary>
        public SolString Intern()
        {
            s_Interned[string.Intern(Value)] = this;
            return this;
        }

        public static implicit operator string(SolString value)
        {
            return value.Value;
        }

        public static implicit operator SolString(string value)
        {
            return ValueOf(value);
        }
    }
}