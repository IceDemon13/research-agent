using System;
using System.Globalization;
using System.Windows;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Common.Converters
{
    public sealed class ResizableLayoutGroupMaxDimensionConverter : MultiValueConverterBase
    {
        public double? OppositeMinDimensionValue { get; set; }

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length != 3)
            {
                throw new ArgumentException("Values must be contains 3 values", nameof(values));
            }

            double dimensionValue = Math.Round(Math.Round((double)values[0]) / 5) * 5;
            double itemSpace = (double)values[1];
            Thickness padding = (Thickness)values[2];

            double result = Math.Max(dimensionValue - (3 * itemSpace) - padding.Left - padding.Right - OppositeMinDimensionValue ?? 100, 0);

            return result;
        }
    }
}