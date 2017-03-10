using JetBrains.Annotations;
using SolScript.Interpreter.Types;

namespace SolScript.Interpreter
{
    /// <summary>
    ///     The <see cref="SolMetaKey" /> class is used to provide a type-safe way to access meta functions.
    /// </summary>
    public abstract class SolMetaKey
    {
        /// <summary>
        ///     Creates a new meta key instance using the given parameters.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="type">The function return type.</param>
        internal SolMetaKey(string name, SolType type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        ///     The name of the functions implementation the meta functions indexable by this key.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The return type of methods using this meta key.
        /// </summary>
        public readonly SolType Type;

        #region Overrides

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals_Impl((SolMetaKey) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return (Type.GetHashCode() * 397) ^ (Name?.GetHashCode() ?? 0);
            }
        }

        #endregion

        /// <summary>
        ///     Equality comparison implementation. Does not check for null.
        /// </summary>
        protected virtual bool Equals_Impl(SolMetaKey other)
        {
            return Type.Equals(other.Type) && string.Equals(Name, other.Name);
        }

        /// <inheritdoc cref="Equals(object)" />
        [Pure]
        public bool Equals(SolMetaKey other)
        {
            if (ReferenceEquals(null, other)) {
                return false;
            }
            if (ReferenceEquals(this, other)) {
                return true;
            }
            return Equals_Impl(other);
        }

        #region Meta Keys

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     The constructor.
        /// </summary>
        public static readonly SolMetaKey<SolNil> __new = new SolMetaKey<SolNil>(nameof(__new), true);

        /// <summary>
        ///     Converts the class to a string.
        /// </summary>
        public static readonly SolMetaKey<SolString> __to_string = new SolMetaKey<SolString>(nameof(__to_string), false);

        /// <summary>
        ///     Counts the class.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __getn = new SolMetaKey<SolNumber>(nameof(__getn), false);

        /// <summary>
        ///     Checks if a class is equal to something.
        /// </summary>
        public static readonly SolMetaKey<SolBool> __is_equal = new SolMetaKey<SolBool>(nameof(__is_equal), false);

        /// <summary>
        ///     Iterates the class.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __iterate = new SolMetaKey<SolTable>(nameof(__iterate), false);

        /// <summary>
        ///     Gets the modulo of a class and something else.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __mod = new SolMetaKey<SolNumber>(nameof(__mod), false);

        /// <summary>
        ///     Expotentiates a class by something.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __exp = new SolMetaKey<SolNumber>(nameof(__exp), false);

        /// <summary>
        ///     Divides a class by something.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __div = new SolMetaKey<SolNumber>(nameof(__div), false);

        /// <summary>
        ///     Adds something to a class.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __add = new SolMetaKey<SolNumber>(nameof(__add), false);

        /// <summary>
        ///     Subtracts something from a class.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __sub = new SolMetaKey<SolNumber>(nameof(__sub), false);

        /// <summary>
        ///     Multiplies a class by something.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __mul = new SolMetaKey<SolNumber>(nameof(__mul), false);

        /// <summary>
        ///     Concatenates something to a class.
        /// </summary>
        public static readonly SolMetaKey<SolString> __concat = new SolMetaKey<SolString>(nameof(__concat), false);

        /// <summary>
        ///     Called before the constructor.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __a_pre_new = new SolMetaKey<SolTable>(nameof(__a_pre_new), false);

        /// <summary>
        ///     Called after the constructor.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __a_post_new = new SolMetaKey<SolTable>(nameof(__a_post_new), false);

        /// <summary>
        ///     Called whenever the linked variable/field is received.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __a_get_variable = new SolMetaKey<SolTable>(nameof(__a_get_variable), false);

        /// <summary>
        ///     Called whenever the linked variable/field is set.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __a_set_variable = new SolMetaKey<SolTable>(nameof(__a_set_variable), false);

        /// <summary>
        ///     Called whenever the linked function is called.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __a_call_function = new SolMetaKey<SolTable>(nameof(__a_call_function), false);

        // ReSharper restore InconsistentNaming

        #endregion
    }

    /// <inheritdoc cref="SolMetaKey" />
    /// <typeparam name="T">This generic parameter defines the return type of the meta function.</typeparam>
    public sealed class SolMetaKey<T> : SolMetaKey where T : SolValue
    {
        internal SolMetaKey(string name, bool canBeNil) : base(name, new SolType(SolValue.PrimitiveTypeNameOf<T>(), canBeNil)) {}

        /// <summary>
        ///     Casts the given <see cref="SolValue" /> to the return value type specified in <typeparamref name="T" />.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <returns>The casted value.</returns>
        /// <remarks>If the <see cref="SolMetaKey.Type" /> can be nil, this method by return <c>null</c> if a nil value was passed.</remarks>
        [CanBeNull]
        public T Cast(SolValue value)
        {
            // todo: the CanBeNull attribute seems to create more trouble that it worth. Maybe find other some way to handle null/nil.
            if (Type.CanBeNil && value == SolNil.Instance) {
                return null;
            }
            return (T) value;
        }
    }
}