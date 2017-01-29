using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types
{
    public abstract class SolValue
    {
        public const string ANY_TYPE = "any";
        public const string CLASS_TYPE = "class";

        public static readonly SolValue[] EmptyArray = new SolValue[0];
        public virtual bool IsClass => false;
        public abstract string Type { get; }

        #region Overrides

        public override string ToString() => ToString_Impl(null);

        #endregion
        
        /// <summary>
        ///     Tries to convert the local value into a value of a C# type. May
        ///     return null.
        /// </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public abstract object ConvertTo(Type type);

        /// <summary>
        ///     Tried to cast the value to another given type using the Convert()
        ///     methods. This method may throw Marshalling Exceptions if the value cannot
        ///     be converted to the target type.
        /// </summary>
        /// <typeparam name="T"> The generic type </typeparam>
        /// <returns> The return type </returns>
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public T ConvertTo<T>()
        {
            return (T) ConvertTo(typeof(T));
        }

        protected abstract string ToString_Impl([CanBeNull] SolExecutionContext context);
        public string ToString([CanBeNull] SolExecutionContext context) => ToString_Impl(context);

       // public new abstract int GetHashCode();
       // public new abstract bool Equals(object other);

        public abstract bool IsEqual(SolExecutionContext context, SolValue other);

        public virtual bool NotEqual(SolExecutionContext context, SolValue other)
        {
            return !IsEqual(context, other);
        }

        public virtual SolNumber Add(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context, $"{Type} does not support addition!");
        }

        public virtual SolValue Subtract(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context, $"{Type} does not support subtraction!");
        }

        public virtual SolValue Multiply(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support multiplication!");
        }

        public virtual SolValue Divide(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support division!");
        }

        public virtual SolValue Exponentiate(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support exponentiating!");
        }

        public virtual SolValue Modulo(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support modulu!");
        }

        public virtual bool SmallerThan(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support smaller than comparison!");
        }

        public virtual bool SmallerThanOrEqual(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support smaller than or equal comparison!");
        }

        public virtual bool GreaterThan(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support greater than comparison!");
        }

        public virtual bool GreaterThanOrEqual(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support greater than or equal comparison!");
        }

        /// <summary>
        ///     Concatenates one value to another. The default implementation returns a new string made from the string
        ///     representation of both values.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <param name="other">The value to concatenate to this value.</param>
        /// <returns>The concatenated value of both values.</returns>
        /// <remarks>
        ///     The method may return anything, altough returning anything but a string may be highly confusing for possible
        ///     users.
        /// </remarks>
        public virtual SolString Concatenate(SolExecutionContext context, SolValue other)
        {
            return new SolString(ToString() + other);
        }

        public virtual SolValue And(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support and!");
        }

        public virtual SolValue Or(SolExecutionContext context, SolValue other)
        {
            throw new SolRuntimeException(context,$"{Type} does not support or!");
        }

        public virtual SolValue Not(SolExecutionContext context)
        {
            throw new SolRuntimeException(context,$"{Type} does not support not!");
        }

        public virtual SolValue Minus(SolExecutionContext context)
        {
            throw new SolRuntimeException(context,$"{Type} does not support minus!");
        }

        public virtual SolValue Plus(SolExecutionContext context)
        {
            throw new SolRuntimeException(context,$"{Type} does not support plus!");
        }

        public virtual SolNumber GetN(SolExecutionContext context)
        {
            throw new SolRuntimeException(context,$"{Type} does not support get n!");
        }

        /// <summary>
        ///     Iterates through the value.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>An iterator object.</returns>
        public virtual IEnumerable<SolValue> Iterate(SolExecutionContext context)
        {
            throw new SolRuntimeException(context,$"{Type} does not support iteration!");
        }

        [DebuggerStepThrough]
        protected static T Sol_HelperThrowNotSupported<T>(SolExecutionContext context, string operation, string val1, string val2) where T: SolValue
        {
            throw new SolRuntimeException(context, $"Tried to {operation} a \"{val1}\" and a \"{val2}\" value.");
        }

        [DebuggerStepThrough]
        protected static bool Bool_HelperThrowNotSupported(SolExecutionContext context, string operation, string val1, string val2)
        {
            throw new SolRuntimeException(context, $"Tried to {operation} a \"{val1}\" and a \"{val2}\" value.");
        }

        /// <summary>
        ///     Note: By default ALL values are true and no values are false. Make
        ///     sure to override these methods to provide customized behaviour.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns> A boolean typically used for condition checks in loops and iterators. </returns>
        public virtual bool IsTrue(SolExecutionContext context)
        {
            return true;
        }

        /// <summary>
        ///     Note: By default ALL values are true and no values are false. Make
        ///     sure to override these methods to provide customized behaviour.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns> A boolean typically used for condition checks in loops and iterators. </returns>
        public virtual bool IsFalse(SolExecutionContext context)
        {
            return false;
        }
    }
}