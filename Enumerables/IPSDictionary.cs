using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSUtility.Enumerables
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// An iterface that implements both <see cref="IDictionary{TKey,TValue}"/> and <see cref="IReadOnlyDictionary{TKey,TValue}"/>
    /// </summary>
    public interface IPSDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        new int Count { get; }
        new bool ContainsKey(TKey key);
        new TValue this[TKey key] { get; set; }
        new IEnumerable<TKey> Keys { get; }
        new IEnumerable<TValue> Values { get; }
        new bool TryGetValue(TKey key, out TValue value);
    }
}
