using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using PSUtility.Enumerables;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types.Interfaces;
using SolScript.Utility;

namespace SolScript.Interpreter.Types
{
    /// <summary>
    ///     The table is used as pure data storage. It allows for key-value pair storage. A key of any type can be mapped to a
    ///     value of any type.
    /// </summary>
    public sealed class SolTable : SolValue, IValueIndexable, IReadOnlyCollection<KeyValuePair<SolValue, SolValue>>
    {
        /// <summary>
        ///     Creates a new empty table.
        /// </summary>
        public SolTable()
        {
            m_Id = s_NextId++;
        }

        /// <inheritdoc cref="SolTable(IEnumerable{SolValue})" />
        public SolTable(params SolValue[] array) : this((IEnumerable<SolValue>) array) {}

        /// <summary>
        ///     Creates a new table from the given enumerable.
        /// </summary>
        /// <param name="array">The enumerable.</param>
        public SolTable(IEnumerable<SolValue> array) : this()
        {
            int i = 0;
            foreach (SolValue value in array) {
                m_Table[new SolNumber(i)] = value;
                i++;
            }
            m_N = i;
        }

        /// <summary>
        ///     The type name is "table".
        /// </summary>
        public const string TYPE = "table";

        // The id of the next table.
        private static uint s_NextId;

        private static readonly SolString s_IteratorKey = SolString.ValueOf("key").Intern();
        private static readonly SolString s_IteratorValue = SolString.ValueOf("value").Intern();
        private readonly uint m_Id;
        private readonly PSUtility.Enumerables.Dictionary<SolValue, SolValue> m_Table = new PSUtility.Enumerables.Dictionary<SolValue, SolValue>();
        private int m_N;

        /// <summary>
        ///     All keys in this table.
        /// </summary>
        public IReadOnlyCollection<SolValue> Keys => m_Table.Keys;

        /// <inheritdoc />
        public override string Type => TYPE;

        /// <summary>
        ///     Wrapper around this[new <see cref="SolNumber" />(<paramref name="index" />)].
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The associated value.</returns>
        public SolValue this[double index] {
            get { return this[new SolNumber(index)]; }
            set { this[new SolNumber(index)] = value; }
        }

        #region IReadOnlyCollection<KeyValuePair<SolValue,SolValue>> Members

        /// <inheritdoc />
        public int Count => m_Table.Count;

        /// <summary> Returns an enumerator that iterates through the table. </summary>
        /// <returns> An enumerator that can be used to iterate through the table. </returns>
        public IEnumerator<KeyValuePair<SolValue, SolValue>> GetEnumerator()
        {
            return m_Table.GetEnumerator();
        }

        /// <summary> Returns an enumerator that iterates through the table. </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be
        ///     used to iterate through the table.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<SolValue, SolValue> item)
        {
            SolValue value;
            return m_Table.TryGetValue(item.Key, out value) && value.Equals(item.Value);
        }

        /// <inheritdoc />
        public void CopyTo(Array array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Count);
        }

        /// <inheritdoc />
        public void CopyTo(Array<KeyValuePair<SolValue, SolValue>> array, int index)
        {
            ArrayUtility.Copy(this, 0, array, index, Count);
        }

        #endregion

        #region IValueIndexable Members

        /// <summary>
        ///     Sets or gets a value from the table. When setting any current value
        ///     will be overridden. <br /> If the key does not exist nil will be returned instead.
        /// </summary>
        /// <param name="key"> The key name </param>
        /// <returns> 'any?' value. </returns>
        /// <exception cref="ArgumentNullException">Tried to index/set a native null value.</exception>
        [NotNull]
        public SolValue this[[NotNull] SolValue key] {
            get {
                if (key == null) {
                    throw new ArgumentNullException(nameof(key), "Tried to index a table by a native null value.");
                }
                SolValue value;
                if (m_Table.TryGetValue(key, out value)) {
                    return value;
                }
                return SolNil.Instance;
            }
            set {
                if (value == null) {
                    throw new ArgumentNullException(nameof(key), "Tried to assign a native null value to a table.");
                }
                if (key == null) {
                    throw new ArgumentNullException(nameof(key), "Tried to index a table by a native null value.");
                }
                if (value.Type == SolNil.TYPE) {
                    m_Table.Remove(key);
                } else {
                    m_Table[key] = value;
                }
            }
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked {
                return 4 + (int) m_Id;
            }
        }

        /// <inheritdoc />
        /// <exception cref="SolMarshallingException"> The value cannot be converted. </exception>
        public override object ConvertTo(Type type)
        {
            if (type == typeof(IValueIndexable)) {
                return this;
            }
            if (type.IsArray) {
                Type elementType = type.GetElementType();
                return CreateArray(elementType);
            }
            // todo: reflection always looks ugly and is slow
            foreach (Type interfce in type.GetInterfaces()) {
                if (interfce.IsGenericType) {
                    Type openGenericInterface = interfce.GetGenericTypeDefinition();
                    if (openGenericInterface == typeof(IList<>)) {
                        return InternalHelper.SandboxCreateObject(type, new object[] {CreateArray(interfce.GetGenericArguments()[0])},
                            (s, exception) => new SolMarshallingException(TYPE, type, "Tried to create as IList - " + s, exception));
                    }
                    if (openGenericInterface == typeof(IDictionary<,>)) {
                        Type[] genericTypes = interfce.GetGenericArguments();
                        Type keyType = genericTypes[0];
                        Type valueType = genericTypes[1];
                        Type genericDictionaryType = typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(keyType, valueType);
                        object dicionaryObj = InternalHelper.SandboxCreateObject<SolMarshallingException>(genericDictionaryType, new object[0], null);
                        IDictionary dictionary = (IDictionary) dicionaryObj;
                        foreach (KeyValuePair<SolValue, SolValue> pair in m_Table) {
                            object key = SolMarshal.MarshalFromSol(pair.Key, keyType);
                            object value = SolMarshal.MarshalFromSol(pair.Value, valueType);
                            dictionary.Add(key, value);
                        }
                        return dicionaryObj;
                    }
                }
            }
            return base.ConvertTo(type);
        }

        /// <summary> Converts the value to a culture specfifc string. </summary>
        protected override string ToString_Impl(SolExecutionContext context)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("table#" + m_Id + " {");
            foreach (KeyValuePair<SolValue, SolValue> kvp in m_Table) {
                {
                    SolTable keyTable = kvp.Key as SolTable;
                    string keyStr;
                    if (keyTable != null) {
                        keyStr = "table#" + keyTable.m_Id;
                    } else {
                        keyStr = kvp.Key.ToString();
                    }
                    SolTable valueTable = kvp.Value as SolTable;
                    string valueStr;
                    if (valueTable != null) {
                        valueStr = "table#" + valueTable.m_Id;
                    } else {
                        valueStr = kvp.Value.ToString();
                    }
                    builder.AppendLine("  " + keyStr + " = " + valueStr);
                }
            }
            builder.AppendLine("}");
            return builder.ToString();
        }

        /*private bool IsEqualRecursive(SolExecutionContext context, SolTable otherTable, PSUtility.Enumerables.List<SolValue> checkedRefs)
        {
            if (m_Id == otherTable.m_Id)
            {
                return true;
            }
            if (Count != otherTable.Count)
            {
                return false;
            }
            if (Count == 0 && otherTable.Count == 0)
            {
                return true;
            }
            foreach (var pair in m_Table)
            {
                SolValue otherValue;
                if (!otherTable.TryGet(pair.Key, out otherValue))
                {
                    return false;
                }
                checkedRefs.Add(otherValue);
                if (!otherValue.IsEqual(context, pair.Value))
                {
                    return false;
                }
            }
        }*/

        /// <inheritdoc />
        public override bool IsEqual(SolExecutionContext context, SolValue other)
        {
            return IsReferenceEqual(context, other);
        }

        /// <inheritdoc />
        public override IEnumerable<SolValue> Iterate(SolExecutionContext context)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (KeyValuePair<SolValue, SolValue> pair in m_Table) {
                yield return new SolTable {
                    [s_IteratorKey] = pair.Key,
                    [s_IteratorValue] = pair.Value
                };
            }
        }

        /// <inheritdoc />
        public override SolNumber GetN(SolExecutionContext context)
        {
            return new SolNumber(m_N);
        }

        #endregion

        /// <summary>
        ///     Creates a table of the given generic enumerable.
        /// </summary>
        /// <typeparam name="T">Enumerable type.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>The table.</returns>
        public static SolTable Of<T>(IEnumerable<T> enumerable) where T : SolValue
        {
            SolTable table = new SolTable();
            int i = 0;
            foreach (T value in enumerable) {
                table.m_Table[new SolNumber(i)] = value;
                i++;
            }
            table.m_N = i;
            return table;
        }

        /// <summary>
        ///     Iterates the array part of this table. Starting at index 0, then index 1, etc. Stops once an index cannot be found.
        ///     Indices may NOT be skipped.
        /// </summary>
        /// <returns>The enumerable.</returns>
        public IEnumerable<SolValue> IterateArray()
        {
            int num = 0;
            while (true) {
                SolValue value;
                if (!m_Table.TryGetValue(new SolNumber(num), out value)) {
                    break;
                }
                yield return value;
                num++;
            }
        }

        /// <summary>
        ///     Clears all data in this <see cref="SolTable" />.
        /// </summary>
        public void Clear()
        {
            m_Table.Clear();
        }

        private Array CreateArray(Type elementType)
        {
            Array array = Array.CreateInstance(elementType, Count);
            int i = 0;
            // todo: only convert numeric part of table to array
            foreach (SolValue value in m_Table.Values) {
                array.SetValue(value.ConvertTo(elementType), i);
                i++;
            }
            return array;
        }

        internal void SetN(int n)
        {
            m_N = n;
        }

        /// <summary>
        ///     Tries to get a <paramref name="value" /> associated with the given <paramref name="key" />.
        /// </summary>
        /// <param name="key">The key to index key.</param>
        /// <param name="value">The value. Will be nil if it does not exist.</param>
        /// <returns>true if the value exists, false if not. </returns>
        /// <remarks>The <paramref name="value" /> is NEVER be null, only nil!</remarks>
        public bool TryGet(SolValue key, [NotNull] out SolValue value)
        {
            if (m_Table.TryGetValue(key, out value)) {
                return true;
            }
            value = SolNil.Instance;
            return false;
        }

        /// <summary>
        ///     Appends a new value to the end to the array structure of this table.
        ///     Returns the new index.
        /// </summary>
        /// <param name="value"> The value. </param>
        /// <returns> The index of the newly added value. Or -1 if the value was nil. </returns>
        public SolNumber Append([NotNull] SolValue value)
        {
            if (value.Type == SolNil.TYPE) {
                return new SolNumber(-1);
            }
            SolNumber key = new SolNumber(m_N);
            while (m_Table.ContainsKey(key)) {
                key = new SolNumber(m_N);
                m_N++;
            }
            m_Table[key] = value;
            m_N++;
            return key;
        }

        /// <summary>
        ///     Checks if the table contains a given key. A table can never contain
        ///     nil values.
        /// </summary>
        /// <param name="key"> The key </param>
        /// <returns> true if the key was found, false if not. </returns>
        public bool Contains(SolValue key)
        {
            return m_Table.ContainsKey(key);
        }

        /// <inheritdoc />
        public override bool IsReferenceEqual(SolExecutionContext context, SolValue other)
        {
            return m_Id == (other as SolTable)?.m_Id;
        }
    }
}