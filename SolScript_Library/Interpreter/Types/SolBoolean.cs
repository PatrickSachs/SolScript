using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;

namespace SolScript.Interpreter.Types {
    public class SolBoolean : SolValue {
        private SolBoolean(bool value) {
            Value = value;
        }

        private const string TRUE_STRING = "true";
        private const string FALSE_STRING = "false";

        public static readonly SolType MarshalFromCSharpType = new SolType("bool", true);

        public static readonly SolBoolean True = new SolBoolean(true);
        public static readonly SolBoolean False = new SolBoolean(false);
        public readonly bool Value;

        public override string Type {
            get { return "bool"; }
            protected set { throw new NotSupportedException("Cannot set type for bools!"); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), DebuggerStepThrough]
        public static SolBoolean ValueOf(bool value) {
            return value ? True : False;
        }

        public static SolBoolean operator !(SolBoolean @this) {
            return new SolBoolean(!@this.Value);
        }

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            //SolDebug.WriteLine("converting solbool " +Value+" to" + type);
            if (type == typeof (bool)) {
                return Value;
            }
            if (type == typeof (bool?)) {
                // SolDebug.WriteLine("null bool");
                return (bool?) Value;
            }
            throw new SolScriptMarshallingException("bool", type, "Cannot convert SolBoolean!");
        }

        protected override string ToString_Impl() {
            return Value ? TRUE_STRING : FALSE_STRING;
        }

        public override bool IsEqual(SolValue other) {
            SolBoolean otherBool = other as SolBoolean;
            return otherBool != null && Value == otherBool.Value;
        }

        public override bool NotEqual(SolValue other) {
            SolBoolean otherBool = other as SolBoolean;
            return otherBool == null || Value != otherBool.Value;
        }

        protected override int GetHashCode_Impl() {
            return 1 + Value.GetHashCode();
        }

        public override bool IsTrue() {
            return Value;
        }

        public override bool IsFalse() {
            return !Value;
        }

        public override SolValue And(SolValue other) {
            SolBoolean otherBool = other as SolBoolean;
            if (otherBool == null) {
                throw new SolScriptInterpreterException("Cannot combine a bool and a " + other.Type + " via and.");
            }
            return new SolBoolean(Value && otherBool.Value);
        }

        public override SolValue Not() {
            return ValueOf(!Value);
        }
    }
}