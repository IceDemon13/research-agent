using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class HexToSolidColorBrushConverter : ValueConverterBase
    {
        private BrushConverter brushConverter;

        public BrushConverter BrushConverter
        {
            get => brushConverter ?? (brushConverter = new BrushConverter());
            set => brushConverter = value;
        }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string colorString = value as string;

            object result;

            if (!string.IsNullOrEmpty(colorString))
            {
                try
                {
                    result = BrushConverter.ConvertFrom($"#{colorString}");
                }
                catch (FormatException)
                {
                    result = DependencyProperty.UnsetValue;
                }
            }
            else
            {
                result = DependencyProperty.UnsetValue;
            }

            return result;
        }
    }
}
