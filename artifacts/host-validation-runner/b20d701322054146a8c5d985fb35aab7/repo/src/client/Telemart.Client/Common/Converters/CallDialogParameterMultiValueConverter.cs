using System;
using System.Globalization;
using System.Linq;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.ViewModels.Dialogs.Call;

namespace Telemart.Client.Common.Converters
{
    public class CallDialogParameterMultiValueConverter : MultiValueConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length > 3 && values.All(x => x == null || x is string || x is int))
            {
                return new CallDialogParameter(
                    values[0] is int ? (int?)values[0] : null,
                    (string)values[1],
                    true,
                    (string)values[2],
                    values.Skip(3).Where(x => x != null).Cast<string>().ToArray());
            }

            return null;
        }
    }
}