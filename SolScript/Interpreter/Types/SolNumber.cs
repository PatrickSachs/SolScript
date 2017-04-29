using System;
using System.Globalization;
using SolScript.Interpreter.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter.Types
{
    public sealed class SolNumber : SolValue
    {
        public SolNumber() : this(0) {}

        public SolNumber(double value)
        {
            MutableValue = value;
        }

        public const string TYPE = "number";

        /// <summary>
        ///     The mutable backing value. This field must be mutable in order for sarcasm to be able to assign its value.
        /// </summary>
        internal double MutableValue;

        public override string Type => TYPE;

        /// <summary>
        ///     The current value represented by this number.
        /// </summary>
        public double Value => MutableValue;

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
            object number;
            if (InternalHelper.TryNumberObject(type, Value, out number)) {
                return number;
            }
            return base.ConvertTo(type);
        }

        protected override string ToString_Impl(SolExecutionContext context)
        {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        public override int GetHashCode()
        {
            unchecked {
                return 2 + Value.GetHashCode();
            }
        }

        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNbr = other as SolNumber;
            return otherNbr != null && Value.Equals(otherNbr.Value);
        }

        public override bool NotEqual(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNbr = other as SolNumber;
            return otherNbr == null || Value.Equals(otherNbr.Value);
        }

        public override bool SmallerThan(SolExecutionContext context, SolValue other)
        {
            if (other.Type != "number") {
                return Bool_HelperThrowNotSupported(context, "compare(smaller)", "number", other.Type);
            }
            SolNumber otherNbr = (SolNumber) other;
            return Value < otherNbr.Value;
        }

        public override bool GreaterThan(SolExecutionContext context, SolValue other)
        {
            if (other.Type != "number") {
                return Bool_HelperThrowNotSupported(context, "compare(greater)", "number", other.Type);
            }
            SolNumber otherNbr = (SolNumber) other;
            return Value > otherNbr.Value;
        }

        public override SolValue Minus(SolExecutionContext context)
        {
            return new SolNumber(-Value);
        }

        public override SolValue Plus(SolExecutionContext context)
        {
            return new SolNumber(+Value);
        }

        public override SolValue Modulo(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Sol_HelperThrowNotSupported<SolValue>(context, "modulo", "number", other.Type);
            }
            try {
                return new SolNumber(Value % otherNumber.Value);
            } catch (ArithmeticException ex) {
                throw new SolRuntimeException(context, "Failed to get the reminder of the division of " + Value + " by " + otherNumber.Value + ".", ex);
            }
        }

        public override SolValue Subtract(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Sol_HelperThrowNotSupported<SolValue>(context, "subtract", "number", other.Type);
            }
            try {
                return new SolNumber(Value - otherNumber.Value);
            } catch (ArithmeticException ex) {
                throw new SolRuntimeException(context, "Failed to subtract number " + Value + " from " + otherNumber.Value + ".", ex);
            }
        }

        public override SolNumber Add(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Sol_HelperThrowNotSupported<SolNumber>(context, "add", "number", other.Type);
            }
            try {
                return new SolNumber(Value + otherNumber.Value);
            } catch (ArithmeticException ex) {
                throw new SolRuntimeException(context, "Failed to add number " + Value + " to " + otherNumber.Value + ".", ex);
            }
        }

        public override SolValue Multiply(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Sol_HelperThrowNotSupported<SolValue>(context, "multiply", "number", other.Type);
            }
            try {
                return new SolNumber(Value * otherNumber.Value);
            } catch (ArithmeticException ex) {
                throw new SolRuntimeException(context, "Failed to multiply number " + Value + " times " + otherNumber.Value + ".", ex);
            }
        }

        public override SolValue Divide(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Sol_HelperThrowNotSupported<SolValue>(context, "multiply", "number", other.Type);
            }
            try {
                return new SolNumber(Value / otherNumber.Value);
            } catch (ArithmeticException ex) {
                throw new SolRuntimeException(context, "Failed to divide number " + Value + " by " + otherNumber.Value + ".", ex);
            }
        }

        public override SolValue Exponentiate(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Sol_HelperThrowNotSupported<SolValue>(context, "exponentiate", "number", other.Type);
            }
            try {
                return new SolNumber(Math.Pow(Value, otherNumber.Value));
            } catch (ArithmeticException ex) {
                throw new SolRuntimeException(context, "Failed to expotentiate " + Value + " by " + otherNumber.Value + ".", ex);
            }
        }

        public override bool SmallerThanOrEqual(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Bool_HelperThrowNotSupported(context, "compare(smaller or equal)", "number", other.Type);
            }
            return Value <= otherNumber.Value;
        }

        public override bool GreaterThanOrEqual(SolExecutionContext context, SolValue other)
        {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Bool_HelperThrowNotSupported(context, "compare(greater or equal)", "number", other.Type);
            }
            return Value >= otherNumber.Value;
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(other, this)) {
                return true;
            }
            if (ReferenceEquals(other, null)) {
                return false;
            }
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return false;
            }
            return otherNumber.Value.Equals(Value);
        }

        #endregion
    }
}