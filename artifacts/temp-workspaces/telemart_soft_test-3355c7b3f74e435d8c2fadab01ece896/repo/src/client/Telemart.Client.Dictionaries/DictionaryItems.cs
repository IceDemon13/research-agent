using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Telemart.Client.Dictionaries
{
    public static class DictionaryItems<T>
        where T : DictionaryItemBase
    {
        public static readonly IReadOnlyCollection<T> Value;

        static DictionaryItems()
        {
            Value = GetValues();
        }

        private static IReadOnlyCollection<T> GetValues()
        {
            Type type = typeof(T);

            return type.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Select(x => x.GetValue(null))
                .Cast<DictionaryItemBase>()
                .OrderBy(x => x.Id)
                .Cast<T>()
                .ToArray();
        }
    }
}