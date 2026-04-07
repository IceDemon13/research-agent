using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Common.Utils;

namespace Telemart.Client.Extensions
{
    public static class EnumerableExtensions
    {
        public static ObservableRangeCollection<T> ToObservableRangeCollection<T>(this IEnumerable<T> source)
        {
            return new ObservableRangeCollection<T>(source);
        }

        public static IEnumerable<IReadOnlyCollection<T>> Section<T>(this IEnumerable<T> source, int length)
        {
            if (length <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            List<T> section = new List<T>(length);

            foreach (T item in source)
            {
                section.Add(item);

                if (section.Count == length)
                {
                    yield return section.AsReadOnly();
                    section = new List<T>(length);
                }
            }

            if (section.Count > 0)
            {
                yield return section.AsReadOnly();
            }
        }

        public static T AllEqualOrDefault<T>(this IEnumerable<T> source, Func<T, T, bool> comparer = null, T defaultValue = default(T))
        {
            comparer = comparer ?? ((x, y) => Equals(x, y));

            T first = source.FirstOrDefault();

            if (Equals(first, defaultValue))
            {
                return defaultValue;
            }

            bool allEqual = source.Skip(1).All(x => comparer(x, first));

            return allEqual ? first : defaultValue;
        }

        public static bool ScrambledEquals<T>(this IEnumerable<T> enumerable1, IEnumerable<T> enumerable2)
        {
            Dictionary<T, int> counter = new Dictionary<T, int>();

            foreach (T x in enumerable1)
            {
                if (counter.ContainsKey(x))
                {
                    counter[x]++;
                }
                else
                {
                    counter.Add(x, 1);
                }
            }

            foreach (T x in enumerable2)
            {
                if (counter.ContainsKey(x))
                {
                    counter[x]--;
                }
                else
                {
                    return false;
                }
            }

            return counter.Values.All(c => c == 0);
        }

        public static IEnumerable<IEnumerable<T>> Transpose<T>(this IEnumerable<IEnumerable<T>> values)
        {
            if (!values.Any())
            {
                return values;
            }

            if (values.First().Count() == 0)
            {
                return Transpose(values.Skip(1));
            }

            T x = values.First().First();
            IEnumerable<T> xs = values.First().Skip(1);
            IEnumerable<IEnumerable<T>> xss = values.Skip(1);
            return new[]
                    {
                        new[] { x }
                            .Concat(xss.Select(ht => ht.First()))
                    }
                    .Concat(new[] { xs }
                        .Concat(xss.Select(ht => ht.Skip(1)))
                        .Transpose());
        }

        public static IReadOnlyCollection<IReadOnlyCollection<T>> GroupByMultiple<T, TRow, TCol>(this IEnumerable<T> source, Func<T, TRow?> rowSelector, Func<T, TCol?> colSelector, int columnCount, int rowCount)
        where TRow : struct
        where TCol : struct
        {
            TCol[] cols = source
                .Select(colSelector)
                .Where(x => x != null)
                .Distinct()
                .Cast<TCol>()
                .ToArray();

            Dictionary<TRow?, List<T>> groups = source
                .GroupBy(rowSelector)
                .Where(x => x.Key != null)
                .ToDictionary(x => x.Key, x => x.ToList());

            List<T> notGrouped = source.Where(x => rowSelector(x) == null).ToList();

            TRow[] rows = groups.Keys.Where(x => x != null).Cast<TRow>().ToArray();

            List<IReadOnlyCollection<T>> result = new List<IReadOnlyCollection<T>>();

            for (int i = 0; i < rowCount; i++)
            {
                List<T> collection = i < rows.Length
                    ? groups[rows[i]]
                    : notGrouped;

                result.Add(GetRow(collection, colSelector, cols, columnCount));
            }

            return result;
        }

        private static IReadOnlyCollection<T> GetRow<T, TCol>(ICollection<T> collection, Func<T, TCol?> colSelector, IReadOnlyCollection<TCol> columns, int columnCount)
            where TCol : struct
        {
            List<T> row = new List<T>();

            foreach (TCol column in columns)
            {
                T item = collection.FirstOrDefault(x => Equals(colSelector(x), column));

                if (item != null)
                {
                    collection.Remove(item);
                }

                row.Add(item);
            }

            for (int i = columns.Count; i < columnCount; i++)
            {
                T item = collection.FirstOrDefault();

                if (item != null)
                {
                    collection.Remove(item);
                }

                row.Add(item);
            }

            return row;
        }
    }
}