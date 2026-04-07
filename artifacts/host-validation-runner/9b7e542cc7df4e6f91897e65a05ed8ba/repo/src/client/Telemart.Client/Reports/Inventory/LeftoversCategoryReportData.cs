using System.Collections.Generic;

namespace Telemart.Client.Reports.Inventory
{
    public class LeftoversCategoryReportData
    {
        public LeftoversCategoryReportData(string categoryName, IReadOnlyCollection<LeftoversProductReportData> products)
        {
            Products = products;
            CategoryName = categoryName;
        }

        public string CategoryName { get; set; }

        public IReadOnlyCollection<LeftoversProductReportData> Products { get; set; }
    }
}
