using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public class StringToColorConverter : ValueConverterBase
    {
        private BrushConverter brushConverter;

        public object Color { get; set; }

        public BrushConverter BrushConverter
        {
            get => brushConverter ?? (brushConverter = new BrushConverter());
            set => brushConverter = value;
        }

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string colorString = value as string;

            object result;

            if (!string.IsNullOrEmpty(colorString) && Color is string colorParameter)
            {
                try
                {
                    result = BrushConverter.ConvertFrom(colorParameter);
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