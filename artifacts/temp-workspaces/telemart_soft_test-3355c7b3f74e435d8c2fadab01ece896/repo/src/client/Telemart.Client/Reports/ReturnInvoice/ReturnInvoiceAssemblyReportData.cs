using System;
using System.Collections.Generic;

namespace Telemart.Client.Reports.ReturnInvoice
{
    public class ReturnInvoiceAssemblyReportData
    {
        public ReturnInvoiceAssemblyReportData(
            List<ReturnInvoiceAssemblyReportCategoryData> categories,
            string warehouseFromName,
            string carryName,
            string supplierName,
            int returnInvoiceId)
        {
            Categories = categories;
            SupplierName = supplierName;
            WarehouseFromName = warehouseFromName;
            CarryName = carryName;
            ReturnInvoiceId = returnInvoiceId;
            DateNow = DateTime.Now.ToString("dd-MM-yyyy HH:mm");
        }

        public int ReturnInvoiceId { get; set; }

        public string WarehouseFromName { get; set; }

        public string CarryName { get; set; }

        public string SupplierName { get; set; }

        public string DateNow { get; set; }

        public List<ReturnInvoiceAssemblyReportCategoryData> Categories { get; set; }
    }
}
