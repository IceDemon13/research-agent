using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Business;

namespace Telemart.Client.Reports.OrderPack
{
    public class ScanSheetReportData
    {
        public ScanSheetReportData(IReadOnlyCollection<ScanSheetOrderReportData> orders)
        {
            Orders = orders;
            TotalPlaces = orders.Sum(x => x.PackagePlaces);
            TotalCostToPay = GetFormattedCost(orders.Sum(x => x.CostToPayUah), orders.Sum(x => x.CostToPayUsd));
        }

        public IReadOnlyCollection<ScanSheetOrderReportData> Orders { get; }

        public int TotalPlaces { get; }

        public string TotalCostToPay { get; }

        private string GetFormattedCost(decimal totalUah, decimal totalUsd)
        {
            string totalFormatted = "---";

            if (totalUah > 0 && totalUsd > 0)
            {
                totalFormatted = $"{CurrencyFormatingRules.ToUahStr(totalUah)}, {CurrencyFormatingRules.ToUsdStr(totalUsd)}";
            }
            else if (totalUsd > 0)
            {
                totalFormatted = CurrencyFormatingRules.ToUsdStr(totalUsd);
            }
            else if (totalUah > 0)
            {
                totalFormatted = CurrencyFormatingRules.ToUahStr(totalUah);
            }

            return totalFormatted;
        }
    }
}