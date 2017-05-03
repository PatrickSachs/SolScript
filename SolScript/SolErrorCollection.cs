using System;
using System.Collections;
using System.Collections.Generic;
using PSUtility.Enumerables;

namespace SolScript
{
    /// <summary>
    ///     A <see cref="SolErrorCollection" /> is used to save <see cref="SolError" />s in an easily accessibly collection.
    /// </summary>
    public sealed class SolErrorCollection : IEnumerable<SolError>
    {
        /// <summary>
        ///     Creates a new read-only collection from the given errors. Use <see cref="CreateCollection" /> if you wish to create
        ///     a non read-only collection.
        /// </summary>
        /// <param name="errors">The errors.</param>
        public SolErrorCollection(IEnumerable<SolError> errors)
        {
            m_Errors = new PSList<SolError>(errors);
        }

        /// <inheritdoc cref="SolErrorCollection(IEnumerable{SolError})" />
        public SolErrorCollection(params SolError[] errors) : this((IEnumerable<SolError>) errors) {}

        private SolErrorCollection()
        {
            m_Errors = new PSList<SolError>();
        }

        private readonly PSList<SolError> m_Errors;

        /// <inheritdoc />
        public int Count => m_Errors.Count;

        /// <summary>
        ///     Checks if this collection has any errors. Warnings count as errors if <see cref="WarningsAreErrors" /> is true.
        /// </summary>
        public bool HasErrors {
            get {
                if (Count > 0) {
                    if (WarningsAreErrors) {
                        return true;
                    }
                    foreach (SolError error in m_Errors) {
                        if (!error.IsWarning) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        ///     Checks if this collection has any warnings. Errors do not count as warnings.
        /// </summary>
        public bool HasWarnings {
            get {
                if (Count > 0) {
                    foreach (SolError error in m_Errors) {
                        if (error.IsWarning) {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        /// <inheritdoc />
        public SolError this[int index] => m_Errors[index];

        /// <summary>
        ///     Are warnings treated as errors by this collection?
        /// </summary>
        public bool WarningsAreErrors { get; private set; }

        #region IEnumerable<SolError> Members

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<SolError> GetEnumerator()
        {
            return m_Errors.GetEnumerator();
        }

        #endregion

        /// <summary>
        ///     Creates a read only list from this error collection.
        /// </summary>
        /// <returns>The read only list.</returns>
        public ReadOnlyList<SolError> AsReadOnly()
        {
            return m_Errors.AsReadOnly();
        }

        /// <inheritdoc />
        public bool Contains(SolError item)
        {
            return m_Errors.Contains(item);
        }

        /// <summary>
        ///     Creates a new error collection from the given parameters.
        /// </summary>
        /// <param name="warningsAreErrors">Are warnings treated are errors by this collection?</param>
        /// <param name="adder">
        ///     This out value is  used to add new errors to the collection. This allows you to expose the
        ///     collection itself in public API without giving the users a way to add new errors.
        /// </param>
        /// <returns>The error collection.</returns>
        public static SolErrorCollection CreateCollection(out Adder adder, bool warningsAreErrors = false)
        {
            SolErrorCollection collection = new SolErrorCollection();
            adder = new Adder(collection);
            return collection;
        }

        /// <inheritdoc />
        public void CopyTo(Array array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Count);
        }

        /// <inheritdoc />
        public void CopyTo(Array<SolError> array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Count);
        }

        #region Nested type: Adder

        /// <summary>
        ///     An adder is used to add new errors to a collection.
        /// </summary>
        public sealed class Adder
        {
            internal Adder(SolErrorCollection collection)
            {
                Collection = collection;
            }

            /// <summary>
            ///     The collection this adder belongs to.
            /// </summary>
            public SolErrorCollection Collection { get; }

            /// <summary>
            ///     Sets if warnings should the treated as errors.
            /// </summary>
            /// <param name="are">Should they be treated are errors?</param>
            public void SetWarningsAreErrors(bool are)
            {
                Collection.WarningsAreErrors = are;
            }

            /// <summary>
            ///     Adds a new error to the collection.
            /// </summary>
            /// <param name="error">The error.</param>
            public void Add(SolError error)
            {
                Collection.m_Errors.Add(error);
            }

            /// <summary>
            ///     Removes an error from the collection.
            /// </summary>
            /// <param name="error">The error.</param>
            /// <returns>Has the error been removed?</returns>
            public bool Remove(SolError error)
            {
                return Collection.m_Errors.Remove(error);
            }

            /// <summary>
            ///     Clears the error collection of all errors.
            /// </summary>
            public void Clear()
            {
                Collection.m_Errors.Clear();
            }
        }

        #endregion
    }
}