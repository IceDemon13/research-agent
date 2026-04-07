using System;
using System.Globalization;
using System.Linq;
using DevExpress.Xpf.Map;
using DevExpress.Xpf.Map.Native;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Views.SalesMap
{
    public class OrdersCountToTextConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int sum = GetSum((value as MapItemInfo).MapItem);

            return sum < 400 ? string.Empty : sum.ToString();
        }

        private int GetSum(MapItem map)
        {
            return map.ClusteredItems.Count == 0
                ? (int)map.Attributes["OrdersCount"].Value
                : map.ClusteredItems.Sum(GetSum);
        }
    }
}
