using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public class CollectionConverter<T> : ValueConverterBase
    {
        public bool ToObservable { get; set; }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IEnumerable<T> values = (IEnumerable<T>)value;

            return values?.Cast<object>().ToList();
        }

        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            IEnumerable<object> values = (IEnumerable<object>)value;

            IReadOnlyCollection<T> mapped = values?.Cast<T>().ToArray() ?? Array.Empty<T>();

            return ToObservable ? mapped.ToObservableCollection() : mapped;
        }
    }

    public class StringCollectionConverter : CollectionConverter<string>
    {
    }

    public class IntCollectionConverter : CollectionConverter<int>
    {
    }
}