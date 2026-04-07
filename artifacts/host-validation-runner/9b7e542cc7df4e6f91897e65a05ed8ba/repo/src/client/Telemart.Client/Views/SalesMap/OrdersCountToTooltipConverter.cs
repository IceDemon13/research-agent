using System;
using System.Globalization;
using System.Linq;
using DevExpress.Xpf.Map;
using DevExpress.Xpf.Map.Native;
using Telemart.Client.Common.Converters.Base;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Helpers;

namespace Telemart.Client.Views.SalesMap
{
    public class OrdersCountToTooltipConverter : ValueConverterBase
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            OrdersCount sum = GetSum((value as MapItemInfo).MapItem);

            string text = $"{sum.Orders} {WordEndingHelper.GetWordByNumber(sum.Orders, "заказ", "заказа", "заказов")}";

            if (sum.Count > 1)
            {
                text += $" в {sum.Count} {WordEndingHelper.GetWordByNumber(sum.Count, "отделении", "отделениях", "отделениях")}";
            }

            return text;
        }

        private OrdersCount GetSum(MapItem map)
        {
            return map.ClusteredItems.Count == 0
                ? new OrdersCount(1, (int)map.Attributes["OrdersCount"].Value)
                : map.ClusteredItems.Select(GetSum).Aggregate((x, y) => x + y);
        }

        private struct OrdersCount
        {
            public OrdersCount(int count, int orders)
            {
                Count = count;
                Orders = orders;
            }

            public int Count { get; }

            public int Orders { get; }

            public static OrdersCount operator +(OrdersCount c1, OrdersCount c2)
            {
                return new OrdersCount(c1.Count + c2.Count, c1.Orders + c2.Orders);
            }
        }
    }
}
