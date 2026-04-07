using System.Collections.Generic;

namespace Telemart.Client.Extensions
{
    public static class CollectionExtensions
    {
        public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
            this Dictionary<TKey, TValue> collection) => collection;
    }
}