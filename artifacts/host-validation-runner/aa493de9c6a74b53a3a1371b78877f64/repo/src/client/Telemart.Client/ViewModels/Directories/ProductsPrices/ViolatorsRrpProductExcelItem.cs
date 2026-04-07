using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Directories.ProductsPrices
{
    public class ViolatorsRrpProductExcelItem
    {
        public ViolatorsRrpProductExcelItem(string productName, Dictionary<int, decimal> competitorPrices, decimal minRrpPrice)
        {
            ProductName = productName;
            CompetitorPrices = competitorPrices;
            MinRrpPrice = minRrpPrice;
        }

        public string ProductName { get; }

        public Dictionary<int, decimal> CompetitorPrices { get; }

        public decimal MinRrpPrice { get; }
    }
}
