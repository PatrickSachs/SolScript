using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types
{
    public sealed class SolBool : SolValue
    {
        private SolBool(bool value)
        {
            Value = value;
        }

        private const string TRUE_STRING = "true";
        private const string FALSE_STRING = "false";
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

        public override SolValue And(SolExecutionContext context, SolValue other)
        {
            SolBool otherBool = other as SolBool;
            if (otherBool == null) {
                throw new SolRuntimeException(context, "Cannot combine a bool and a " + other.Type + " via and.");
            }
            return new SolBool(Value && otherBool.Value);
        }

        public override SolValue Not(SolExecutionContext context)
        {
            return ValueOf(!Value);
        }

        public override SolValue Or(SolExecutionContext context, SolValue other)
        {
            SolBool otherBool = other as SolBool;
            if (otherBool == null) {
                throw new SolRuntimeException(context, "Cannot or switch a bool and a " + other.Type + " via and.");
            }
            return new SolBool(Value || otherBool.Value);
        }

        public override bool Equals(object other)
        {
            // There are only two SolBool instances, so reference compare is perfectly fine.
            return other == this;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static SolBool ValueOf(bool value)
        {
            return value ? True : False;
        }

        public static SolBool operator !(SolBool @this)
        {
            return @this.Value ? False : True;
        }
    }
}