using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types {
    public abstract class SolValue {
        public const string ANY_TYPE = "any";
        public const string NIL_TYPE = "nil";

        public static readonly SolValue[] EmptyArray = new SolValue[0];

        public abstract string Type { get; protected set; }

        //private int hashCode => GetHashCode();

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public abstract object ConvertTo(Type type);

        /// <summary> Tried to cast the value to another given type using the Convert()
        ///     methods. This method may throw Marshalling Exceptions if the value cannot
        ///     be converted to the target type. </summary>
        /// <typeparam name="T"> The generic type </typeparam>
        /// <returns> The return type </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public T ConvertTo<T>() {
            return (T) ConvertTo(typeof (T));
        }

        protected abstract string ToString_Impl();
        protected abstract int GetHashCode_Impl();
        //protected abstract bool Equals_Impl([CanBeNull] object value);

        public override int GetHashCode() => GetHashCode_Impl();
        public override string ToString() => ToString_Impl();

        public override bool Equals([CanBeNull] object value) {
            if (ReferenceEquals(null, value)) return false;
            if (ReferenceEquals(this, value)) return true;
            if (value.GetType() != GetType()) return false;
            return IsEqual((SolValue) value);
        }

        public abstract bool IsEqual(SolValue other);

        public virtual bool NotEqual(SolValue other) {
            return !IsEqual(other);
        }

        public virtual SolValue Add(SolValue other) {
            throw new NotSupportedException(Type + " does not support addition!");
        }

        public virtual SolValue Subtract(SolValue other) {
            throw new NotSupportedException(Type + " does not support subtraction!");
        }

        public virtual SolValue Multiply(SolValue other) {
            throw new NotSupportedException(Type + " does not support multiplication!");
        }

        public virtual SolValue Divide(SolValue other) {
            throw new NotSupportedException(Type + " does not support division!");
        }

        public virtual SolValue Exponentiate(SolValue other) {
            throw new NotSupportedException(Type + " does not support exponentiating!");
        }

        public virtual SolValue Modulu(SolValue other) {
            throw new NotSupportedException(Type + " does not support modulu!");
        }

        public virtual bool SmallerThan(SolValue other) {
            throw new NotSupportedException(Type + " does not support smaller than comparison!");
        }

        public virtual bool SmallerThanOrEqual(SolValue other) {
            throw new NotSupportedException(Type + " does not support smaller than or equal comparison!");
        }

        public virtual bool GreaterThan(SolValue other) {
            throw new NotSupportedException(Type + " does not support greater than comparison!");
        }

        public virtual bool GreaterThanOrEqual(SolValue other) {
            throw new NotSupportedException(Type + " does not support greater than or equal comparison!");
        }

        public virtual SolValue Concatenate(SolValue other) {
            return new SolString(ToString() + other);
        }

        public virtual SolValue And(SolValue other) {
            throw new NotSupportedException(Type + " does not support and!");
        }

        public virtual SolValue Or(SolValue other) {
            throw new NotSupportedException(Type + " does not support or!");
        }

        public virtual SolValue Not() {
            throw new NotSupportedException(Type + " does not support not!");
        }

        public virtual SolValue Minus() {
            throw new NotSupportedException(Type + " does not support minus!");
        }

        public virtual SolValue Plus() {
            throw new NotSupportedException(Type + " does not support plus!");
        }

        public virtual SolValue GetN() {
            throw new NotSupportedException(Type + " does not support get n!");
        }

        public virtual IEnumerable<SolValue> Iterate() {
            throw new NotSupportedException(Type + " does not support iteration!");
        }

        [DebuggerStepThrough]
        protected static SolValue Nil_HelperThrowNotSupported(string operation, string val1, string val2) {
            throw new NotSupportedException("Tried to " + operation + " a " + val1 + " with a " + val2 +
                                            " - This is not supported.");
        }

        [DebuggerStepThrough]
        protected static bool Bool_HelperThrowNotSupported(string operation, string val1, string val2) {
            throw new NotSupportedException("Tried to " + operation + " a " + val1 + " with a " + val2 +
                                            " - This is not supported.");
        }

        /// <summary> Note: By default ALL values are true and no values are false. Make
        ///     sure to override these methods to provide customized behaviour. </summary>
        /// <returns> A boolean typically used for condition checks in loops and iterators. </returns>
        public virtual bool IsTrue() {
            return true;
        }

        /// <summary> Note: By default ALL values are true and no values are false. Make
        ///     sure to override these methods to provide customized behaviour. </summary>
        /// <returns> A boolean typically used for condition checks in loops and iterators. </returns>
        public virtual bool IsFalse() {
            return false;
        }
    }
}