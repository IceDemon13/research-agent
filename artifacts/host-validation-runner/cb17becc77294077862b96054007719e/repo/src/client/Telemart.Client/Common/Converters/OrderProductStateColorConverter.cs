using System;
using System.Globalization;
using System.Windows.Media;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Common.Converters
{
    public class OrderProductStateColorConverter : MultiValueConverterBase
    {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Brush color = null;

            if (values[0] is OrderProductStatus state
                && values[1] is bool isOnWarehouse)
            {
                int? orderResponsibleId = values[2] as int?;

                if (isOnWarehouse)
                {
                    color = Brushes.Green;
                }
                else if (state == OrderProductStatus.Clarify && orderResponsibleId.HasValue)
                {
                    color = Brushes.Red;
                }
            }

            return color;
        }
    }
}