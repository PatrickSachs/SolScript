using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using SolScript.Interpreter.Exceptions;
using SolScript.Interpreter.Types.Interfaces;

namespace SolScript.Interpreter.Types {
    public class SolTable : SolValue, IValueIndexable, IEnumerable<KeyValuePair<SolValue, SolValue>> {
        public SolTable() {
            m_Id = s_NextId++;
        }

        private static uint s_NextId;

        private static readonly SolString s_IteratorKey = new SolString("key");
        private static readonly SolString s_IteratorValue = new SolString("value");
        private readonly uint m_Id;
        private readonly Dictionary<SolValue, SolValue> m_Table = new Dictionary<SolValue, SolValue>();
        private int m_N;

        public override string Type => "table";

        public int Count => m_Table.Count;

        public SolValue this[[NotNull] string key] {
            get { return this[new SolString(key)]; }
            set { this[new SolString(key)] = value; }
        }

        public SolValue this[double key] {
            get { return this[new SolNumber(key)]; }
            set { this[new SolNumber(key)] = value; }
        }

        public SolValue this[bool key] {
            get { return this[SolBool.ValueOf(key)]; }
            set { this[SolBool.ValueOf(key)] = value; }
        }
        
        #region IEnumerable<KeyValuePair<SolValue,SolValue>> Members

        /// <summary> Returns an enumerator that iterates through the table. </summary>
        /// <returns> An enumerator that can be used to iterate through the table. </returns>
        public IEnumerator<KeyValuePair<SolValue, SolValue>> GetEnumerator() {
            return m_Table.GetEnumerator();
        }

        /// <summary> Returns an enumerator that iterates through the table. </summary>
        /// <returns> An <see cref="T:System.Collections.IEnumerator"/> object that can be
        ///     used to iterate through the table. </returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        #endregion

        #region IValueIndexable Members

        /// <summary> Sets or gets a value from the table. When setting any current value
        ///     will be overridden. When getting if the given key exists its value will be
        ///     returned. If the key does not exist a nil value will be returned instead.
        ///     This indexer will NOT throw on non existant keys. </summary>
        /// <param name="key"> The key name </param>
        /// <returns> 'any?' value. </returns>
        [NotNull]
        public SolValue this[[NotNull] SolValue key] {
            get {
                SolValue value;
                if (m_Table.TryGetValue(key, out value)) {
                    return value;
                }
                return SolNil.Instance;
            }
            set {
#if DEBUG
                if (value == null) {
                    throw new ArgumentNullException(nameof(value),
                        "Debug->Tried to set a null value as table element. This is NOT allowed!");
                }
#endif
                bool valueIsNil = value.Type == SolNil.TYPE;
                SolValue currentValue;
                if (m_Table.TryGetValue(key, out currentValue)) {
                    if (valueIsNil) {
                        m_Table.Remove(key);
                    } else {
                        m_Table[key] = value;
                    }
                } else if (!valueIsNil) {
                    m_Table[key] = value;
                }
            }
        }

        #endregion

        #region Overrides

        protected override int GetHashCode_Impl() {
            unchecked {
                return 4 + (int) m_Id;
            }
        }

        /// <summary> Tries to convert the local value into a value of a C# type. May
        ///     return null. </summary>
        /// <param name="type"> The target type </param>
        /// <returns> The object </returns>
        /// <exception cref="SolScriptMarshallingException"> The value cannot be converted. </exception>
        [CanBeNull]
        public override object ConvertTo(Type type) {
            if (type == typeof (SolValue) || type == typeof (SolTable) || type == typeof (IValueIndexable)) {
                return this;
            }
            throw new SolScriptMarshallingException("table", type);
        }

        /// <summary> Converts the value to a culture specfifc string. </summary>
        protected override string ToString_Impl([CanBeNull] SolExecutionContext context) {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("table#" + m_Id + " {");
            foreach (var kvp in m_Table) {
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

        public override bool IsEqual(SolExecutionContext context, SolValue other) {
            if (other.Type != "table") {
                return false;
            }
            SolTable otherTable = (SolTable) other;
            return m_Id == otherTable.m_Id;
        }

        public override IEnumerable<SolValue> Iterate(SolExecutionContext context) {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var pair in m_Table) {
                yield return new SolTable {
                    [s_IteratorKey] = pair.Key,
                    [s_IteratorValue] = pair.Value
                };
            }
        }

        public override SolValue GetN(SolExecutionContext context) {
            return new SolNumber(m_N);
        }

        #endregion

        internal void SetN(int n) {
            m_N = n;
        }

        [CanBeNull]
        public SolValue GetIfDefined(string key) {
            return GetIfDefined(new SolString(key));
        }

        [CanBeNull]
        public SolValue GetIfDefined(SolValue key) {
            SolValue value;
            return m_Table.TryGetValue(key, out value)
                ? value
                : null;
        }

        /// <summary> Appends a new value to the end to the array structure of this table.
        ///     Returns the new index. </summary>
        /// <param name="value"> The value </param>
        /// <returns> The index of the newly added value </returns>
        public SolNumber Append([NotNull] SolValue value) {
            SolNumber key = new SolNumber(m_N);
            while (m_Table.ContainsKey(key)) {
                key = new SolNumber(m_N);
                m_N++;
            }
            m_Table[key] = value;
            m_N++;
            return key;
        }

        /// <summary> Checks if the table contains a given key. A table can never contain
        ///     nil values. </summary>
        /// <param name="key"> The key </param>
        /// <returns> true if the key was found, false if not. </returns>
        public bool Contains(SolValue key) {
            return m_Table.ContainsKey(key);
        }

        public SolValue[] ToArray() {
            var array = new SolValue[m_N];
            for (int i = 0; i < m_N; i++) {
                array[i] = this[new SolNumber(i)];
            }
            return array;
        }
    }
}