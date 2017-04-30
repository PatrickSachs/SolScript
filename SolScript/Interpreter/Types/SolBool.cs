using System;
using System.Diagnostics;
using SolScript.Interpreter.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter.Types
{
    public sealed class SolBool : SolValue
    {
        private SolBool(bool value)
        {
            Value = value;
        }

        public const string TRUE_STRING = "true";
        public const string FALSE_STRING = "false";
        public const string TYPE = "bool";

        public static readonly SolBool True = new SolBool(true);
        public static readonly SolBool False = new SolBool(false);
        public readonly bool Value;

        public override string Type => TYPE;

        #region Overrides

        /// <summary>
        ///     Tries to convert the local value into a value of a C# type. May
        ///     return null.
        /// </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            if (type == typeof(bool)) {
                return Value;
            }
            if (type == typeof(bool?)) {
                return (bool?) Value;
            }
            object number;
            if (InternalHelper.TryNumberObject(type, Value ? 1 : 0, out number)) {
                return number;
            }
            return base.ConvertTo(type);
        }

        protected override string ToString_Impl(SolExecutionContext context)
        {
            return Value ? TRUE_STRING : FALSE_STRING;
        }

        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            SolBool otherBool = other as SolBool;
            return otherBool != null && Value == otherBool.Value;
        }

        public override bool NotEqual(SolExecutionContext context, SolValue other)
        {
            SolBool otherBool = other as SolBool;
            return otherBool == null || Value != otherBool.Value;
        }

        public override int GetHashCode()
        {
            return 1 + Value.GetHashCode();
        }

        public override bool IsTrue(SolExecutionContext context)
        {
            return Value;
        }

        public override bool IsFalse(SolExecutionContext context)
        {
            return !Value;
        }

        public override SolValue Not(SolExecutionContext context)
        {
            return ValueOf(!Value);
        }

        public override bool Equals(object other)
        {
            // There are only two SolBool instances, so reference compare is perfectly fine.
            return other == this;
        }

        /// <inheritdoc />
        public override bool IsReferenceEqual(SolExecutionContext context, SolValue other)
        {
            return IsEqual(context, other);
        }

        #endregion

        /// <summary>
        ///     Gets the SolScript boolean representation of the given bool value.
        /// </summary>
        /// <param name="value">The native bool.</param>
        /// <returns>The SolScript bool.</returns>
        /// <remarks>This method exists to ensure that there are only two SolBool instances and to ease usablity.</remarks>
        [DebuggerStepThrough]
        public static SolBool ValueOf(bool value)
        {
            return value ? True : False;
        }

        /// <summary>
        ///     Negates the bool.
        /// </summary>
        /// <param name="this">The bool.</param>
        /// <returns>If the bool was true, it returns false, otherwise true.</returns>
        public static SolBool operator !(SolBool @this)
        {
            return @this.Value ? False : True;
        }

        /// <summary>
        ///     Returns the <see cref="Value" /> of the bool.
        /// </summary>
        /// <param name="this">The bool.</param>
        public static implicit operator bool(SolBool @this)
        {
            return @this.Value;
        }

        /// <summary>
        ///     Creates a <see cref="SolBool" /> matching to the given bool value.
        /// </summary>
        /// <param name="this">The bool.</param>
        public static implicit operator SolBool(bool @this)
        {
            return ValueOf(@this);
        }
    }
}