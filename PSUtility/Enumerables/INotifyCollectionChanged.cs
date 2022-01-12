using System.ComponentModel;

namespace PSUtility.Enumerables
{
    public interface INotifyCollectionChanged
    {
        event CollectionChangedEventHandler CollectionChanged;
    }
}