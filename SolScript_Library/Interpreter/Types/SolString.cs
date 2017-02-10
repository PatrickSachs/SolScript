using System;
using System.Collections.Generic;
using System.Text;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types
{
    /// <summary>
    ///     The <see cref="SolString" /> is used to represent well... a string. As opposed to may other languages a string is
    ///     an actual primite in SolScript. There is not character type. Characters are either represented by a string with a
    ///     length of one or a number.
    /// </summary>
    public sealed class SolString : SolValue
    {
        static SolString()
        {
            // Intern some often used values.
            Empty.Intern();
            new SolString(" ").Intern();
            new SolString(SolBool.TRUE_STRING).Intern();
            new SolString(SolBool.FALSE_STRING).Intern();
            new SolString("key").Intern();
            new SolString("value").Intern();
            new SolString("index").Intern();
            new SolString("length").Intern();
            new SolString("override").Intern();
            new SolString("new_args").Intern();
        }

        // Private constrcutor to support interning.
        private SolString(string value)
        {
            Value = value;
        }

        public const string TYPE = "string";

        private static readonly Dictionary<string, SolString> Interned = new Dictionary<string, SolString>();

        /// <summary>
        ///     An empty("") string.
        /// </summary>
        public static readonly SolString Empty = new SolString(string.Empty);

        /// <summary>
        ///     The current value of this string.
        /// </summary>
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

        /// <summary>
        ///     Gets the <see cref="SolString" /> of the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The created string.</returns>
        public static SolString ValueOf(string value)
        {
            SolString str;
            if (Interned.TryGetValue(value, out str)) {
                return str;
            }
            return new SolString(value);
        }

        /// <summary>
        ///     Interns the string. Interned strings only exist once in memory and thus safe memory if they are expected to exist
        ///     very often within your application.
        /// </summary>
        public void Intern()
        {
            Interned.Add(string.Intern(Value), this);
        }
    }
}