using System;
using System.Reflection;
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
        ///     Creates a new meta key instance using the given parameteters.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="type">The function return type.</param>
        internal SolMetaKey(string name, SolType type)
        {
            Name = name;
            Type = type;
        }

        public static readonly SolMetaKey<SolNil> Constructor = new SolMetaKey<SolNil>("__new", true);
        public static readonly SolMetaKey<SolString> AsString = new SolMetaKey<SolString>("__to_string", false);
        public static readonly SolMetaKey<SolNumber> GetN = new SolMetaKey<SolNumber>("__get_n", false);
        public static readonly SolMetaKey<SolBool> IsEqual = new SolMetaKey<SolBool>("__is_equal", false);
        public static readonly SolMetaKey<SolTable> Iterate = new SolMetaKey<SolTable>("__iterate", false);
        public static readonly SolMetaKey<SolNumber> Modulo = new SolMetaKey<SolNumber>("__modulo", false);
        public static readonly SolMetaKey<SolNumber> Expotentiate = new SolMetaKey<SolNumber>("__expotentiate", false);
        public static readonly SolMetaKey<SolNumber> Divide = new SolMetaKey<SolNumber>("__divide", false);
        public static readonly SolMetaKey<SolNumber> Add = new SolMetaKey<SolNumber>("__add", false);
        public static readonly SolMetaKey<SolNumber> Subtract = new SolMetaKey<SolNumber>("__subtract", false);
        public static readonly SolMetaKey<SolNumber> Multiply = new SolMetaKey<SolNumber>("__multiply", false);
        public static readonly SolMetaKey<SolString> Concatenate = new SolMetaKey<SolString>("__concatenate", false);
        public static readonly SolMetaKey<SolTable> AnnotationPreConstructor = new SolMetaKey<SolTable>("__a_pre_new", false);
        public static readonly SolMetaKey<SolTable> AnnotationPostConstructor = new SolMetaKey<SolTable>("__a_post_new", false);
        public static readonly SolMetaKey<SolTable> AnnotationGetVariable = new SolMetaKey<SolTable>("__a_get_variable", false);
        public static readonly SolMetaKey<SolTable> AnnotationSetVariable = new SolMetaKey<SolTable>("__a_set_variable", false);
        public static readonly SolMetaKey<SolTable> AnnotationCallFunction = new SolMetaKey<SolTable>("__a_call_function", false);

        /// <summary>
        ///     The name of the functions implementation the meta functions indexable by this key.
        /// </summary>
        public readonly string Name;

        /// <summary>
        ///     The return type of methods using this meta key.
        /// </summary>
        public readonly SolType Type;

        #region Overrides

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

        public override int GetHashCode()
        {
            unchecked {
                return (Type.GetHashCode() * 397) ^ (Name?.GetHashCode() ?? 0);
            }
        }

        #endregion

        protected virtual bool Equals_Impl(SolMetaKey other)
        {
            return Type.Equals(other.Type) && string.Equals(Name, other.Name);
        }

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
    }

    /// <inheritdoc cref="SolMetaKey" />
    /// <typeparam name="T">This generic parameter defines the return type of the meta function.</typeparam>
    public sealed class SolMetaKey<T> : SolMetaKey where T : SolValue
    {
        // todo: provide a better way to statically access the type name without or with cached reflection.
        internal SolMetaKey(string name, bool canBeNil)
            : base(
                name,
                new SolType(
                    (string)
                    typeof(T).GetField("TYPE", BindingFlags.Static | BindingFlags.Public).NotNull("Every SolValue type needs to have a public const string field named TYPE.").GetValue(null).NotNull(),
                    canBeNil)) {}

        /// <summary>
        ///     Casts the given <see cref="SolValue" /> to the return value type specified in <typeparamref name="T" />.
        /// </summary>
        /// <param name="value">The value to cast.</param>
        /// <returns>The casted value.</returns>
        /// <remarks>If the <see cref="SolMetaKey.Type"/> can be nil, this method by return <c>null</c> if a nil value was passed.</remarks>
        [CanBeNull]
        public T Cast(SolValue value)
        {
            if (Type.CanBeNil && value == SolNil.Instance) {
                return null;
            }
            return (T) value;
        }
    }
}