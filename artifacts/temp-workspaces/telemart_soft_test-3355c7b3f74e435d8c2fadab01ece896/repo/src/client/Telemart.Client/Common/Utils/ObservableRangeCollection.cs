using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Telemart.Client.Common.Utils
{
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        private const string CountString = "Count";
        private const string IndexerName = "Item[]";

        public ObservableRangeCollection()
        {
        }

        public ObservableRangeCollection(IEnumerable<T> collection)
            : base(collection)
        {
        }

        public ObservableRangeCollection(List<T> list)
            : base(list)
        {
        }

        protected enum ProcessRangeAction
        {
            Add,
            Replace,
            Remove
        }

        public void AddRange(IEnumerable<T> collection)
        {
            ProcessRange(collection, ProcessRangeAction.Add);
        }

        public void ReplaceRange(IEnumerable<T> collection)
        {
            ProcessRange(collection, ProcessRangeAction.Replace);
        }

        public void RemoveRange(IEnumerable<T> collection)
        {
            ProcessRange(collection, ProcessRangeAction.Remove);
        }

        public void Sort<TKey>(Func<T, TKey> orderFunc)
        {
            IEnumerable<T> items = Items.OrderBy(orderFunc).ToList();

            ProcessRange(items, ProcessRangeAction.Replace);
        }

        public void SortDescending<TKey>(Func<T, TKey> orderFunc)
        {
            IEnumerable<T> items = Items.OrderByDescending(orderFunc).ToList();

            ProcessRange(items, ProcessRangeAction.Replace);
        }

        protected virtual void ProcessRange(IEnumerable<T> collection, ProcessRangeAction action)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            IList<T> items = collection as IList<T> ?? collection.ToList();

            if (!items.Any())
            {
                return;
            }

            CheckReentrancy();

            if (action == ProcessRangeAction.Replace)
            {
                Items.Clear();
            }

            foreach (T item in items)
            {
                switch (action)
                {
                    case ProcessRangeAction.Remove:
                        Items.Remove(item);
                        break;
                    default:
                        Items.Add(item);
                        break;
                }
            }

            OnPropertyChanged(new PropertyChangedEventArgs(CountString));
            OnPropertyChanged(new PropertyChangedEventArgs(IndexerName));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}