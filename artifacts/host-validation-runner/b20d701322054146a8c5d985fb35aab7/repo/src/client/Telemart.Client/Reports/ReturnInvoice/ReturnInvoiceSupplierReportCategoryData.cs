using System.Collections.Generic;

namespace Telemart.Client.Reports.ReturnInvoice
{
    public class ReturnInvoiceSupplierReportCategoryData
    {
        public ReturnInvoiceSupplierReportCategoryData(string categoryName, List<ReturnInvoiceSupplierReportProductData> products)
        {
            Products = products;
            CategoryName = categoryName;
        }

        public string CategoryName { get; set; }

        public List<ReturnInvoiceSupplierReportProductData> Products { get; set; }
    }
}
