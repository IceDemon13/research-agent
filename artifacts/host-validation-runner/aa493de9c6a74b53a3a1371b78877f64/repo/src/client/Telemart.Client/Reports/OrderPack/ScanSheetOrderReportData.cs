using System;
using Telemart.Client.Business;

namespace Telemart.Client.Reports.OrderPack
{
    public class ScanSheetOrderReportData
    {
        public ScanSheetOrderReportData(string id, DateTime? dateX, string fio, string phone, string phone2, string address, string comment, decimal costToPayUah, decimal costToPayUsd, int packagePlaces)
        {
            Id = id;
            DateX = dateX;
            Fio = fio;
            Phone = GetFormattedPhones(phone, phone2);
            Address = address;
            Comment = comment;
            PackagePlaces = packagePlaces;
            CostToPayFormatted = GetFormattedCost(costToPayUah, costToPayUsd);
            CostToPayUsd = costToPayUsd;
            CostToPayUah = costToPayUah;
        }

        public string Id { get; }

        public DateTime? DateX { get; }

        public string Fio { get; }

        public string Phone { get; }

        public string Address { get; }

        public string Comment { get; }

        public string CostToPayFormatted { get; }

        public int PackagePlaces { get; }

        public decimal CostToPayUah { get; }

        public decimal CostToPayUsd { get; }

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

        private string GetFormattedPhones(string phone, string phone2)
        {
            if (string.IsNullOrEmpty(phone) && string.IsNullOrEmpty(phone2))
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(phone2))
            {
                return $"{phone},     {phone2}";
            }

            return string.IsNullOrEmpty(phone2) ? phone : phone2;
        }
    }
}
