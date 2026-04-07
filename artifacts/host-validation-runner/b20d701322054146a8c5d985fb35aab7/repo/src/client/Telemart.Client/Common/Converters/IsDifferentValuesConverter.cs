using System;
using System.Globalization;
using System.Windows;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Converters
{
    public class IsDifferentValuesConverter : MultiValueConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
            {
                return null;
            }

            UnsetToNull(values);

            return !values[0].Same(values[1]);
        }

        private static void UnsetToNull(params object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == DependencyProperty.UnsetValue)
                {
                    objects[i] = null;
                }
            }
        }
    }
}
