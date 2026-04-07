using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public sealed class CompetitorPriceKoefToColorConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object result = DependencyProperty.UnsetValue;

            decimal? k = value as decimal?;

            if (k != null)
            {
                if (k.Value < 0.98m)
                {
                    result = Brushes.LightGreen;
                }
                else
                {
                    if (k.Value > 1.0001m)
                    {
                        result = Brushes.LightSkyBlue;
                    }

                    if (k.Value > 1.012m)
                    {
                        result = Brushes.LightPink;
                    }
                }
            }

            return result;
        }
    }
}