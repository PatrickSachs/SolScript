using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Types;
using SolScript.Utility;

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
        /// <param name="parameterData">The parameter types of the meta function. (null if any are allowed)</param>
        internal SolMetaKey(string name, SolType type, [CanBeNull] ParameterData parameterData)
        {
            Name = name;
            Type = type;
            Parameters = parameterData;
        }

        /// <summary>
        ///     The name of the functions implementation the meta functions indexable by this key.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The parameters of this meta function. null if any are allowed.
        /// </summary>
        [CanBeNull] public readonly ParameterData Parameters;

        /// <summary>
        ///     The return type of this meta function.
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

        #region Nested type: ParameterData

        /// <summary>
        ///     The parameter data class is used to convey information about the desired parameters of a meta function.
        /// </summary>
        public class ParameterData
        {
            /// <summary>Creates a new parameter data instance.</summary>
            /// <param name="allowOptional">Are optional parameters allowed?</param>
            /// <param name="parameters">The parameter types of the meta function.</param>
            internal ParameterData(bool allowOptional, params SolType[] parameters)
            {
                AllowOptional = allowOptional;
                m_Parameters = new Array<SolType>(parameters);
            }

            internal static readonly ParameterData Empty = new ParameterData(false);
            internal static readonly ParameterData OnlyOptional = new ParameterData(true);
            internal static readonly ParameterData AnyFirstArgument = new ParameterData(false, SolType.AnyNil);
            internal static readonly ParameterData AnyFirstAndSecondArgument = new ParameterData(false, SolType.AnyNil, SolType.AnyNil);

            /// <summary>
            ///     Are optional parameters allowed?
            /// </summary>
            public readonly bool AllowOptional;

            // Backing params type array.
            private readonly Array<SolType> m_Parameters;

            /// <summary>
            ///     The parameter types of the meta function.
            /// </summary>
            public IReadOnlyList<SolType> Types => m_Parameters;
        }

        #endregion

        #region Meta Keys

        // ReSharper disable InconsistentNaming
        /// <summary>
        ///     The constructor.
        /// </summary>
        public static readonly SolMetaKey<SolNil> __new = new SolMetaKey<SolNil>(nameof(__new), true, null);

        /// <summary>
        ///     Converts the class to a string.
        /// </summary>
        public static readonly SolMetaKey<SolString> __to_string = new SolMetaKey<SolString>(nameof(__to_string), false, ParameterData.Empty);

        /// <summary>
        ///     Counts the class.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __getn = new SolMetaKey<SolNumber>(nameof(__getn), false, ParameterData.Empty);

        /// <summary>
        ///     Checks if a class is equal to something.
        /// </summary>
        public static readonly SolMetaKey<SolBool> __is_equal = new SolMetaKey<SolBool>(nameof(__is_equal), false, ParameterData.AnyFirstArgument);

        /// <summary>
        ///     Iterates the class.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __iterate = new SolMetaKey<SolTable>(nameof(__iterate), false, ParameterData.Empty);

        /// <summary>
        ///     Gets the modulo of a class and something else.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __mod = new SolMetaKey<SolNumber>(nameof(__mod), false, ParameterData.AnyFirstArgument);

        /// <summary>
        ///     Expotentiates a class by something.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __exp = new SolMetaKey<SolNumber>(nameof(__exp), false, ParameterData.AnyFirstArgument);

        /// <summary>
        ///     Divides a class by something.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __div = new SolMetaKey<SolNumber>(nameof(__div), false, ParameterData.AnyFirstArgument);

        /// <summary>
        ///     Adds something to a class.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __add = new SolMetaKey<SolNumber>(nameof(__add), false, ParameterData.AnyFirstArgument);

        /// <summary>
        ///     Subtracts something from a class.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __sub = new SolMetaKey<SolNumber>(nameof(__sub), false, ParameterData.AnyFirstArgument);

        /// <summary>
        ///     Multiplies a class by something.
        /// </summary>
        public static readonly SolMetaKey<SolNumber> __mul = new SolMetaKey<SolNumber>(nameof(__mul), false, ParameterData.AnyFirstArgument);

        /// <summary>
        ///     Concatenates something to a class.
        /// </summary>
        public static readonly SolMetaKey<SolString> __concat = new SolMetaKey<SolString>(nameof(__concat), false, ParameterData.AnyFirstArgument);

        /// <summary>
        ///     Called before the constructor.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __a_pre_new = new SolMetaKey<SolTable>(nameof(__a_pre_new), false, ParameterData.AnyFirstAndSecondArgument);

        /// <summary>
        ///     Called after the constructor.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __a_post_new = new SolMetaKey<SolTable>(nameof(__a_post_new), false, ParameterData.AnyFirstAndSecondArgument);

        /// <summary>
        ///     Called whenever the linked variable/field is received.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __a_get_variable = new SolMetaKey<SolTable>(nameof(__a_get_variable), false, ParameterData.AnyFirstAndSecondArgument);

        /// <summary>
        ///     Called whenever the linked variable/field is set.
        /// </summary>
        public static readonly SolMetaKey<SolTable> __a_set_variable = new SolMetaKey<SolTable>(nameof(__a_set_variable), false, ParameterData.AnyFirstAndSecondArgument);

        // ReSharper restore InconsistentNaming

        #endregion
    }

    /// <inheritdoc cref="SolMetaKey" />
    /// <typeparam name="T">This generic parameter defines the return type of the meta function.</typeparam>
    public sealed class SolMetaKey<T> : SolMetaKey where T : SolValue
    {
        /// <summary>
        ///     Creates a new meta key instance using the given parameters.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="canBeNil">Can the function return nil?</param>
        /// <param name="parameterData">The parameter types of the meta function. (null if any are allowed)</param>
        internal SolMetaKey(string name, bool canBeNil, ParameterData parameterData) : base(name, new SolType(SolValue.PrimitiveTypeNameOf<T>(), canBeNil), parameterData) {}

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