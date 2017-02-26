using SolScript.Interpreter;
using SolScript.Interpreter.Types;

namespace SolScript
{
    /// <summary>
    ///     Represets a value mapped to a certain type.
    /// </summary>
    public struct ValueTypePair
    {
        /// <summary>
        ///     The value.
        /// </summary>
        public readonly SolValue Value;

        /// <summary>
        ///     The type.
        /// </summary>
        public readonly SolType Type;

        /// <summary>
        ///     Creates a new value type pair.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        public ValueTypePair(SolValue value, SolType type)
        {
            Value = value;
            Type = type;
        }

        /// <inheritdoc />
        public bool Equals(ValueTypePair other)
        {
            return Equals(Value, other.Value) && Type.Equals(other.Type);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            return obj is ValueTypePair && Equals((ValueTypePair) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ Type.GetHashCode();
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return "Value: " + Value + " (Type: " + Type + ")";
        }

        #region Operators

        /// <summary>
        ///     Compares using the <see cref="Equals(SolScript.ValueTypePair)" /> method.
        /// </summary>
        /// <param name="o1">Object 1 to compare.</param>
        /// <param name="o2">Object 2 to compare.</param>
        /// <returns>true if both values are considered equal.</returns>
        public static bool operator ==(ValueTypePair o1, object o2)
        {
            return o1.Equals(o2);
        }

        /// <summary>
        ///     Compares using the <see cref="Equals(SolScript.ValueTypePair)" /> method.
        /// </summary>
        /// <param name="o1">Object 1 to compare.</param>
        /// <param name="o2">Object 2 to compare.</param>
        /// <returns>true if both values are considered equal.</returns>
        public static bool operator ==(object o1, ValueTypePair o2)
        {
            return o2.Equals(o1);
        }

        /// <summary>
        ///     Compares using the <see cref="Equals(SolScript.ValueTypePair)" /> method.
        /// </summary>
        /// <param name="o1">Object 1 to compare.</param>
        /// <param name="o2">Object 2 to compare.</param>
        /// <returns>true if both values are not considered equal.</returns>
        public static bool operator !=(ValueTypePair o1, object o2)
        {
            return !o1.Equals(o2);
        }

        /// <summary>
        ///     Compares using the <see cref="Equals(SolScript.ValueTypePair)" /> method.
        /// </summary>
        /// <param name="o1">Object 1 to compare.</param>
        /// <param name="o2">Object 2 to compare.</param>
        /// <returns>true if both values are not considered equal.</returns>
        public static bool operator !=(object o1, ValueTypePair o2)
        {
            return !o2.Equals(o1);
        }

        /// <summary>
        ///     Compares using the <see cref="Equals(SolScript.ValueTypePair)" /> method.
        /// </summary>
        /// <param name="o1">Object 1 to compare.</param>
        /// <param name="o2">Object 2 to compare.</param>
        /// <returns>true if both values are considered equal.</returns>
        public static bool operator ==(ValueTypePair o1, ValueTypePair o2)
        {
            return o1.Equals(o2);
        }

        /// <summary>
        ///     Compares using the <see cref="Equals(SolScript.ValueTypePair)" /> method.
        /// </summary>
        /// <param name="o1">Object 1 to compare.</param>
        /// <param name="o2">Object 2 to compare.</param>
        /// <returns>true if both values are not considered equal.</returns>
        public static bool operator !=(ValueTypePair o1, ValueTypePair o2)
        {
            return !o1.Equals(o2);
        }

        #endregion
    }
}