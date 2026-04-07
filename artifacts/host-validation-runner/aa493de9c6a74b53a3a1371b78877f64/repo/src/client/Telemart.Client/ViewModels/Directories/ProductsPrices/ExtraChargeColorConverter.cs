using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class ExtraChargeConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object result = false;

            decimal? charge = value as decimal?;

            if (charge != null && charge.Value < 1)
            {
                result = true;
            }

            return result;
        }
    }
}