using System.Collections.Generic;
using Telemart.Client.Core.Extensions;


namespace Telemart.Client.ReportDesigner
{
    public class MovementProductReportData
    {
        public MovementProductReportData() { }

        public MovementProductReportData(int? productId, string productName, int? position, int? productsCount, int? orderId, int? inStock, string barcode)
        {
            ProductName = productName;
            ProductsCount = productsCount;
            Position = position;
            OrderId = orderId;
            InStock = inStock;
            ProductId = productId;
            PartBarcode = barcode.GetLastPartSubString(4);
        }

        public int? ProductId { get; set; }

        public string ProductName { get; set; }

        public int? ProductsCount { get; set; }

        public int? InStock { get; set; }

        public int? Position { get; set; }

        public string PartBarcode { get; set; }

        public IEnumerable<MovementProductReportData> ChildNodes { get; set; }

        public int? OrderId { get; set; }
    }
}