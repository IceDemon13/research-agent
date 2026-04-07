using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Telemart.Client.Common
{
    public class KeyCollectionItem<TKey, TItem> : IEquatable<KeyCollectionItem<TKey, TItem>>
    {
        public KeyCollectionItem(TKey key, IReadOnlyCollection<TItem> items)
        {
            Key = key;
            Items = items;
        }

        public TKey Key { get; }

        public IReadOnlyCollection<TItem> Items { get; }

        public bool Equals([AllowNull] KeyCollectionItem<TKey, TItem> other)
        {
            return other != null && EqualityComparer<TKey>.Default.Equals(Key, other.Key)
                && other.Items.All(x => Items.Contains(x))
                && Items.All(x => other.Items.Contains(x));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as KeyCollectionItem<TKey, TItem>);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode()
                ^ (Items.Count > 0
                    ? Items
                        .Select(x => x.GetHashCode())
                        .Aggregate((x, y) => x ^ y)
                    : 0);
        }
    }
}