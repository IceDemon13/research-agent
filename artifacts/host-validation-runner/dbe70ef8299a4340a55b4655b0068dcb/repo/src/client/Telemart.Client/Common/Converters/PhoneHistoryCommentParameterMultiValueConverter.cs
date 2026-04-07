using System;
using System.Globalization;
using System.Linq;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Common.PhoneHistory;
using Telemart.Client.Common.PrintSticker;
using Telemart.Client.Extensions;

namespace Telemart.Client.Common.Converters
{
    public class PhoneHistoryCommentParameterMultiValueConverter : MultiValueConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length >= 1)
            {
                return new PhoneHistoryCommentParameter(values.GetValueOrDafault<string>(0), values.GetValueOrDafault<int?>(1), values.GetValueOrDafault<int?>(2));
            }

            return null;
        }
    }
}