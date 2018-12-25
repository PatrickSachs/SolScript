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
        private readonly BiDictionary<int, T> m_Data = new BiDictionary<int, T>();
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
        ///     Tries to find the ID the given value is assoicated with.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The ID, or -1 if none could be found.</returns>
        public int FindIdOf(T value)
        {
            int id;
            if (m_Data.TryGetValue(value, out id)) {
                return id;
            }
            return -1;
        }

        /// <summary>
        ///     Creates a read only lookup from this mapping.
        /// </summary>
        public ReadOnlyDictionary<int, T> AsReadOnly() => m_Data.Value1AsReadOnly();

        /// <summary>
        ///     Clears the id mapping.
        /// </summary>
        public void Clear()
        {
            m_Data.Clear();
            m_MiddleIds.Clear();
            m_NextTopId = 0;
        }

        /// <summary>
        ///     Claims an id.
        /// </summary>
        /// <returns>The claimed id.</returns>
        public int Claim()
        {
            if (m_MiddleIds.Count != 0) {
                int id = m_MiddleIds[0];
                m_MiddleIds.RemoveAt(0);
                return id;
            }
            return m_NextTopId++;
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

        /// <summary>
        ///     Assigns a value to the given id.
        /// </summary>
        /// <param name="handle">jThe id.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentException">The ID has not been claimed.</exception>
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
    }
}