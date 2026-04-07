using System;
using System.Globalization;
using System.Linq;
using DevExpress.Xpf.Map;
using DevExpress.Xpf.Map.Native;
using Telemart.Client.Common.Converters.Base;

namespace Telemart.Client.Views.SalesMap
{
    public class OrdersCountToRadiusConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double sum = GetSum((value as MapItemInfo).MapItem) + 50;

            return Math.Sqrt(sum / Math.PI);
        }

        private int GetSum(MapItem map)
        {
            return map.ClusteredItems.Count == 0
                ? (int)map.Attributes["OrdersCount"].Value
                : map.ClusteredItems.Sum(GetSum);
        }
    }
}
