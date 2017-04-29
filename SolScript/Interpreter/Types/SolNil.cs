using System;
using SolScript.Interpreter.Exceptions;
using SolScript.Utility;

namespace SolScript.Interpreter.Types
{
    public sealed class SolNil : SolValue
    {
        private SolNil() {}

        public const string TYPE = "nil";

        public static readonly SolNil Instance = new SolNil();

        /// <inheritdoc />
        public override string Type => TYPE;

        #region Overrides

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            if (type.IsClass) {
                return null;
            }
            if (type == typeof(bool)) {
                return false;
            }
            object number;
            if (InternalHelper.TryNumberObject(type, 0, out number, true, true)) {
                return number;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                return Activator.CreateInstance(type);
            }
            return base.ConvertTo(type);
        }

        /// <inheritdoc />
        protected override string ToString_Impl(SolExecutionContext context)
        {
            return "nil";
        }

        /// <inheritdoc />
        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            return other.Type == TYPE;
        }

        /// <inheritdoc />
        public override bool NotEqual(SolExecutionContext context, SolValue other)
        {
            return other.Type != TYPE;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return 0;
        }

        /// <inheritdoc />
        public override bool IsTrue(SolExecutionContext context)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool IsFalse(SolExecutionContext context)
        {
            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object other)
        {
            return other == this;
        }

        /// <inheritdoc />
        public override bool IsReferenceEqual(SolExecutionContext context, SolValue other)
        {
            return other is SolNil;
        }

        #endregion
    }
}