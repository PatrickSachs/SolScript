using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace PSUtility.Metadata
{
    /// <summary>
    ///     Non generic base class for meta keys. New keys must extend the generic meta key.
    /// </summary>
    /// <remarks>The base class only exists to allow easier storage in collections such as dictionaries.</remarks>
    public abstract class MetaKeyBase : IComparable<MetaKeyBase>
    {
        /// <summary>
        ///     Creates a new meta key.
        /// </summary>
        /// <param name="name">The key name.</param>
        internal MetaKeyBase(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     The key name. Meta keys with the same name should be considered as equal in e.g. dictionaries regardless of their
        ///     type.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The data type associated with this meta key.
        /// </summary>
        public abstract Type DataType { get; }

        public static IEqualityComparer<MetaKeyBase> NameComparer { get; } = new NameEqualityComparer();
        public static IEqualityComparer<MetaKeyBase> NameAndTypeComparer { get; } = new NameAndTypeEqualityComparer();

        /// <inheritdoc />
        public int CompareTo(MetaKeyBase other)
        {
            if (ReferenceEquals(this, other)) {
                return 0;
            }
            if (ReferenceEquals(null, other)) {
                return 1;
            }
            return string.Compare(Name, other.Name, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        protected bool Equals(MetaKeyBase other)
        {
            return string.Equals(Name, other.Name) && DataType == other.DataType;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            MetaKeyBase other = obj as MetaKeyBase;
            if (other == null) {
                return false;
            }
            return Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode() + DataType.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString() => Name;

        private sealed class NameEqualityComparer : IEqualityComparer<MetaKeyBase>
        {
            public bool Equals(MetaKeyBase x, MetaKeyBase y)
            {
                if (ReferenceEquals(x, y)) {
                    return true;
                }
                if (ReferenceEquals(x, null)) {
                    return false;
                }
                if (ReferenceEquals(y, null)) {
                    return false;
                }
                if (x.GetType() != y.GetType()) {
                    return false;
                }
                return string.Equals(x.Name, y.Name);
            }

            public int GetHashCode(MetaKeyBase obj)
            {
                return obj.Name != null ? obj.Name.GetHashCode() : 0;
            }
        }

        private sealed class NameAndTypeEqualityComparer : IEqualityComparer<MetaKeyBase>
        {
            public bool Equals(MetaKeyBase x, MetaKeyBase y)
            {
                if (ReferenceEquals(x, y)) {
                    return true;
                }
                if (ReferenceEquals(x, null)) {
                    return false;
                }
                if (ReferenceEquals(y, null)) {
                    return false;
                }
                if (x.GetType() != y.GetType()) {
                    return false;
                }
                return string.Equals(x.Name, y.Name) && x.DataType == y.DataType;
            }

            public int GetHashCode(MetaKeyBase obj)
            {
                return obj.Name.GetHashCode() + obj.DataType.GetHashCode();
            }
        }
    }

    /// <summary>
    ///     This type is used as a key to access <see cref="IMetaDataProvider" />s.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    [PublicAPI]
    public class MetaKey<T> : MetaKeyBase
    {
        /// <inheritdoc />
        public MetaKey(string name) : base(name) {}

        /// <inheritdoc />
        public override Type DataType => typeof(T);
    }
}