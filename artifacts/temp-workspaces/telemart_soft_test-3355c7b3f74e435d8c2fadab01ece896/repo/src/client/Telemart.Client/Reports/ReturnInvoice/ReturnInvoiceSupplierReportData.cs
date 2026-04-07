using System.Collections.Generic;

namespace Telemart.Client.Reports.ReturnInvoice
{
    public class ReturnInvoiceSupplierReportData
    {
        public ReturnInvoiceSupplierReportData(
            List<ReturnInvoiceSupplierReportCategoryData> categories,
            string warehouseFromName,
            string carryName,
            string supplierName,
            int returnInvoiceId,
            string createdOn)
        {
            Categories = categories;
            SupplierName = supplierName;
            WarehouseFromName = warehouseFromName;
            CarryName = carryName;
            ReturnInvoiceId = returnInvoiceId;
            CreatedOn = createdOn;
        }

        public int ReturnInvoiceId { get; set; }

        public string WarehouseFromName { get; set; }

        public string CarryName { get; set; }

        public string SupplierName { get; set; }

        public string CreatedOn { get; set; }

        public List<ReturnInvoiceSupplierReportCategoryData> Categories { get; set; }
    }
}
