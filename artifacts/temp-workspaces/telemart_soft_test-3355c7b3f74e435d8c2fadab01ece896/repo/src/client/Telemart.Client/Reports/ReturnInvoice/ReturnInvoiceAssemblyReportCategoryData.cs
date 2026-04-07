using System.Collections.Generic;

namespace Telemart.Client.Reports.ReturnInvoice
{
    public class ReturnInvoiceAssemblyReportCategoryData
    {
        public ReturnInvoiceAssemblyReportCategoryData(string categoryName, List<ReturnInvoiceAssemblyReportProductData> products)
        {
            Products = products;
            CategoryName = categoryName;
        }

        public string CategoryName { get; set; }

        public List<ReturnInvoiceAssemblyReportProductData> Products { get; set; }
    }
}
