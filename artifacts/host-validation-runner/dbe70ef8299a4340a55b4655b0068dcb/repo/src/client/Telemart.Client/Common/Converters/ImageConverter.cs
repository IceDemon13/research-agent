using System;
using System.Globalization;
using System.Windows.Media.Imaging;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class ImageConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string uriString = (string)value;

            BitmapImage image = null;

            if (!string.IsNullOrWhiteSpace(uriString))
            {
                image = new BitmapImage(new Uri(uriString, UriKind.Absolute));
            }

            return image;
        }
    }
}