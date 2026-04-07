using System;
using System.Globalization;
using System.Linq;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Common.PrintSticker;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Converters
{
    public class PrintStickerParameterMultiValueConverter : MultiValueConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length >= 1 && values.All(x => x == null || x is int || x is bool))
            {
                return new PrintStickerParameter(values.GetValueOrDafault<int?>(0), values.GetValueOrDafault<bool>(1, true));
            }

            return null;
        }
    }
}