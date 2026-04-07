using System;
using System.Globalization;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Store.Order;

namespace Telemart.Client.Views.Common
{
    public class ClientContactTypeConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object res = null;

            if (value is ClientContactViewItem item)
            {
                if (item.Type == ClientContactType.Call)
                {
                    res = item.CallState;
                }

                if (item.Type == ClientContactType.Sms || item.Type == ClientContactType.Viber)
                {
                    res = item.SmsState;
                }

                if (item.Type == ClientContactType.Email)
                {
                    res = item.EmailState;
                }
            }

            return res;
        }
    }
}
