using Telemart.Client.Core.Extensions;

namespace Telemart.Client.Reports.Order.Assembly
{
    public class OrderAssemblyProductReportData
    {
        public OrderAssemblyProductReportData(int productId, string name, int quantity, int warehouseQuantity, int warehouseQuantityFree, string barcode)
        {
            Name = name;
            Quantity = quantity;
            WarehouseQuantity = warehouseQuantity;
            WarehouseQuantityFree = warehouseQuantityFree;
            ProductId = productId;
            PartBarcode = barcode.GetLastPartSubString(4);
        }

        public int ProductId { get; }

        public string Name { get; }

        public int Quantity { get; }

        public int WarehouseQuantity { get; }

        public int WarehouseQuantityFree { get; }

        public string PartBarcode { get; }
    }
}