using System;
using System.Collections.Generic;
using System.Globalization;

namespace Telemart.Client.ReportDesigner1
{
    public sealed class OrderWarrantyCardReportData
    {
        public OrderWarrantyCardReportData(
            int orderId,
            DateTime issueDate,
            IReadOnlyCollection<OrderProductWarrantyCardReportData> products)
        {
            Products = products;
            Header = $"До замовлення №{orderId.ToString(CultureInfo.InvariantCulture)} від {issueDate:dd:MM:yyyy}";
        }

        public string Header { get; }

        public IReadOnlyCollection<OrderProductWarrantyCardReportData> Products { get; }
    }
}