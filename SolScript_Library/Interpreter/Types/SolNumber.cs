using System;
using System.Globalization;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types {
    public class SolNumber : SolValue {
        public SolNumber(double value) {
            Value = value;
        }

        public readonly double Value;

        public override string Type => "number";

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            if (type == typeof (SolValue) || type == typeof (SolNumber)) {
                return this;
            }
            if (type == typeof (double)) {
                return Value;
            }
            if (type == typeof (float)) {
                return (float) Value;
            }
            if (type == typeof (int)) {
                return (int) Value;
            }
            if (type == typeof (uint)) {
                return (uint) Value;
            }
            if (type == typeof (long)) {
                return (long) Value;
            }
            if (type == typeof (ulong)) {
                return (ulong) Value;
            }
            if (type == typeof (byte)) {
                return (byte) Value;
            }
            if (type == typeof (short)) {
                return (short) Value;
            }
            if (type == typeof (ushort)) {
                return (ushort) Value;
            }
            throw new SolScriptMarshallingException("number", type);
        }

        protected override string ToString_Impl([CanBeNull]SolExecutionContext context) {
            return Value.ToString(CultureInfo.InvariantCulture);
        }

        protected override int GetHashCode_Impl() {
            unchecked {
                return 2 + Value.GetHashCode();
            }
        }

        public override bool IsEqual(SolExecutionContext context, SolValue other) {
            SolNumber otherNbr = other as SolNumber;
            return otherNbr != null && Value == otherNbr.Value;
        }

        public override bool NotEqual(SolExecutionContext context, SolValue other) {
            SolNumber otherNbr = other as SolNumber;
            return otherNbr == null || Value != otherNbr.Value;
        }

        public override bool SmallerThan(SolExecutionContext context, SolValue other) {
            if (other.Type != "number") {
                return Bool_HelperThrowNotSupported("compare(smaller)", "number", other.Type);
            }
            SolNumber otherNbr = (SolNumber) other;
            return Value < otherNbr.Value;
        }

        public override bool GreaterThan(SolExecutionContext context, SolValue other) {
            if (other.Type != "number") {
                return Bool_HelperThrowNotSupported("compare(greater)", "number", other.Type);
            }
            SolNumber otherNbr = (SolNumber) other;
            return Value > otherNbr.Value;
        }

        public override SolValue Minus(SolExecutionContext context) {
            return new SolNumber(-Value);
        }

        public override SolValue Plus(SolExecutionContext context) {
            return new SolNumber(+Value);
        }

        public override SolValue Modulu(SolExecutionContext context, SolValue other) {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Nil_HelperThrowNotSupported("modulu", "number", other.Type);
            }
            return new SolNumber(Value%otherNumber.Value);
        }

        public override SolValue Subtract(SolExecutionContext context, SolValue other) {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Nil_HelperThrowNotSupported("subtract", "number", other.Type);
            }
            return new SolNumber(Value - otherNumber.Value);
        }

        public override SolValue Add(SolExecutionContext context, SolValue other) {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Nil_HelperThrowNotSupported("add", "number", other.Type);
            }
            return new SolNumber(Value + otherNumber.Value);
        }

        public override SolValue Multiply(SolExecutionContext context, SolValue other) {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Nil_HelperThrowNotSupported("multiply", "number", other.Type);
            }
            return new SolNumber(Value*otherNumber.Value);
        }

        public override SolValue Divide(SolExecutionContext context, SolValue other) {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Nil_HelperThrowNotSupported("multiply", "number", other.Type);
            }
            if (otherNumber.Value == 0) {
                throw new SolScriptInterpreterException(context, "Tried to divide " + Value + " by zero!");
            }
            return new SolNumber(Value/otherNumber.Value);
        }

        public override SolValue Exponentiate(SolExecutionContext context, SolValue other) {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Nil_HelperThrowNotSupported("exponentiate", "number", other.Type);
            }
            return new SolNumber(Math.Pow(Value, otherNumber.Value));
        }

        public override bool SmallerThanOrEqual(SolExecutionContext context, SolValue other) {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Bool_HelperThrowNotSupported("compare(smaller or equal)", "number", other.Type);
            }
            return Value <= otherNumber.Value;
        }

        public override bool GreaterThanOrEqual(SolExecutionContext context, SolValue other) {
            SolNumber otherNumber = other as SolNumber;
            if (otherNumber == null) {
                return Bool_HelperThrowNotSupported("compare(greater or equal)", "number", other.Type);
            }
            return Value >= otherNumber.Value;
        }
    }
}