using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace Telemart.Client.Common.Converters
{
    [ContentProperty("Converters")]
    public class ConverterGroup : IValueConverter
    {
        public ObservableCollection<IValueConverter> Converters { get; } = new ObservableCollection<IValueConverter>();

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object output = value;
            Type currentTargetType = typeof(object);

            foreach (IValueConverter converter in Converters)
            {
                output = converter.Convert(output, currentTargetType, parameter, culture);
            }

            return output;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object output = value;
            Type currentTargetType = typeof(object);

            foreach (IValueConverter converter in Converters.Reverse())
            {
                output = converter.ConvertBack(output, currentTargetType, parameter, culture);
            }

            return output;
        }
    }
}
