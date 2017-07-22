using System;
using System.Collections;
using System.Collections.Generic;

namespace PSUtility.Enumerables
{
    /// <summary>
    ///     Allows to map objects to an id. Allows allows for id reuse even in the middle of the map.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    public class IdMapping<T> : IEnumerable<KeyValuePair<int, T>>
    {
        // The actual map.
        private readonly PSDictionary<int, T> m_Data = new PSDictionary<int, T>();
        // The ids avilable in the middle of the data.
        private readonly PSList<int> m_MiddleIds = new PSList<int>();
        // The top id that will keep increaing.
        private int m_NextTopId;

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
        {
            return m_Data.GetEnumerator();
        }

        /// <summary>
        ///     Creates a read only lookup from this mapping.
        /// </summary>
        public ReadOnlyDictionary<int, T> AsReadOnly() => m_Data.AsReadOnly();

        /// <summary>
        ///     Clears the id mapping.
        /// </summary>
        public void Clear()
        {
            m_Data.Clear();
            m_MiddleIds.Clear();
            m_NextTopId = 0;
        }

        private int ClaimId()
        {
            if (m_MiddleIds.Count != 0) {
                int id = m_MiddleIds[0];
                m_MiddleIds.RemoveAt(0);
                return id;
            }
            return m_NextTopId++;
        }

        public int Claim()
        {
            return ClaimId();
        }

        /// <summary>
        ///     Releases the given handle from this mapping.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <returns>true if the value was released, false if not.</returns>
        public bool Release(int handle)
        {
            if (!IsValid(handle)) {
                return false;
            }
            if (!m_Data.Remove(handle)) {
                return false;
            }
            m_MiddleIds.Add(handle);
            return true;
        }

        /// <summary>
        ///     Tries to get the value associated with the given handle.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <param name="value">The valuee.</param>
        /// <returns>true if the handle was mapped to a value, false if not.</returns>
        public bool TryGet(int handle, out T value)
        {
            return m_Data.TryGetValue(handle, out value);
        }

        public void Assign(int handle, T value)
        {
            if (!IsValid(handle)) {
                throw new ArgumentException("Invalid handle " + handle);
            }
            m_Data[handle] = value;
        }

        /// <summary>
        ///     Checks if the given handle is valid/claimed.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <returns>true if the handle is valid, false if not.</returns>
        public bool IsValid(int handle)
        {
            return !m_MiddleIds.Contains(handle) && handle < m_NextTopId;
        }

        /*public class ClaimHandle
        {
            /// <inheritdoc />
            /// <exception cref="ArgumentNullException"><paramref name="mapping" /> is <see langword="null" /></exception>
            public ClaimHandle(int id, IdMapping<T> mapping)
            {
                if (mapping == null) {
                    throw new ArgumentNullException(nameof(mapping));
                }
                Id = id;
                Mapping = mapping;
            }

            public int Id { get; }
            public bool Valid => !Mapping.m_MiddleIds.Contains(Id) && Id < Mapping.m_NextTopId;
            public IdMapping<T> Mapping { get; }
        }*/
    }
}