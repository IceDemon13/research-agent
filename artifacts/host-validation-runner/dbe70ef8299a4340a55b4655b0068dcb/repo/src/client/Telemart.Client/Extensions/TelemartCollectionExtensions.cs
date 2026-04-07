using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;

namespace Telemart.Client.Extensions
{
    public static class TelemartCollectionExtensions
    {
        public static void RemoveAll<T>(this ICollection<T> source, Func<T, bool> predicate)
        {
            for (int i = 0; i < source.Count; i++)
            {
                T element = source.ElementAt(i);

                if (predicate(element))
                {
                    source.Remove(element);
                    i--;
                }
            }
        }

        public static T RemoveAtAndGetNext<T>(this ICollection<T> source, int index)
        {
            if (source == null)
            {
                return default(T);
            }

            T element = source.ElementAt(index);

            source.Remove(element);

            if (source.Count > 0)
            {
                return index <= source.Count - 1
                    ? source.ElementAt(index)
                    : source.ElementAt(index - 1);
            }

            return default(T);
        }

        public static void DoActionWithItem<T>(this ICollection<T> source, Func<T, bool> predicate, Action<T> action)
        {
            if (source == null)
            {
                return;
            }

            T item = source.FirstOrDefault(predicate);

            if (item != null)
            {
                action(item);
            }
        }

        public static void ModeItemDown<T>(this ObservableCollection<T> collection, T item)
        {
            int index = collection.IndexOf(item);

            if (index < collection.Count - 1)
            {
                collection.Move(index, index + 1);
            }
        }

        public static void ModeItemUp<T>(this ObservableCollection<T> collection, T item)
        {
            int index = collection.IndexOf(item);

            if (index > 0)
            {
                collection.Move(index, index - 1);
            }
        }

        public static void CheckItems(this IReadOnlyCollection<ICheckableTreeItem> items, IReadOnlyCollection<int> checkedIds)
        {
            HashSet<int> parentItemIds = new HashSet<int>();

            items.ForEach(x => x.Checked = false);

            foreach (ICheckableTreeItem item in items.Where(x => items.All(y => y.ParentId != x.Id)))
            {
                if (checkedIds.Contains(item.Id))
                {
                    item.Checked = true;

                    if (item.ParentId.HasValue)
                    {
                        parentItemIds.Add(item.ParentId.Value);
                    }
                }
            }

            RecursiveCheckItems(items, parentItemIds);
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                collection.Add(item);
            }
        }

        public static IReadOnlyCollection<T> NullIfEmpty<T>(this IReadOnlyCollection<T> items)
        {
            return items.Count == 0
                ? null
                : items;
        }

        private static void RecursiveCheckItems(IReadOnlyCollection<ICheckableTreeItem> items, HashSet<int> checkedIds)
        {
            if (checkedIds.Any())
            {
                HashSet<int> parentItemIds = new HashSet<int>();

                foreach (ICheckableTreeItem item in items.Where(x => checkedIds.Contains(x.Id)))
                {
                    if (items.Where(x => x.ParentId == item.Id).All(x => x.Checked == true))
                    {
                        item.Checked = true;
                    }
                    else
                    {
                        item.Checked = null;
                    }

                    if (item.ParentId.HasValue)
                    {
                        parentItemIds.Add(item.ParentId.Value);
                    }
                }

                RecursiveCheckItems(items, parentItemIds);
            }
        }
    }
}