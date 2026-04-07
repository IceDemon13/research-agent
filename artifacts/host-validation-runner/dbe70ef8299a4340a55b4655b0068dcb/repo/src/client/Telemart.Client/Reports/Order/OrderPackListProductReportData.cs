using System.Collections.Generic;
using Telemart.Client.Core.Extensions;

namespace Telemart.Client.Reports.Order
{
    public class OrderPackListProductReportData
    {
        public int? SequencePosition { get; set; }

        public int? ProductId { get; set; }

        public string Name { get; set; }

        public int? Quantity { get; set; }

        public string PartBarcode { get; set; }

        public int? OrderId { get; set; }

        public int? WarehouseQuantity { get; set; }

        public int? WarehouseQuantityFree { get; set; }

        public IEnumerable<OrderPackListProductReportData> ChildProducts { get; set; }
    }
}