using System;
using System.Collections.Generic;
using Telemart.Client.Business;
using Telemart.Client.Dictionaries;
using Telemart.Client.ReportDesigner;

namespace Telemart.Client.Reports.Order
{
    public sealed class OrderReportData
    {
        public OrderReportData(
            int id,
            decimal deliveryCost,
            decimal prepaymentCostUah,
            decimal prepaymentCostUsd,
            string headerTop,
            string headerBottom,
            string seller,
            bool isFooterVisible,
            IReadOnlyCollection<OrderPropertyReportData> properties,
            IReadOnlyCollection<OrderProductReportData> products,
            string customerFio)
        {
            if (deliveryCost < 0)
            {
                throw new ArgumentException("Delivery cost less then zero ");
            }

            Id = id;
            DeliveryCostDecimal = deliveryCost;
            PrepaymentCostUah = prepaymentCostUah;
            PrepaymentCostUsd = prepaymentCostUsd;
            HeaderTop = headerTop;
            HeaderBottom = headerBottom;

            Header = $"{HeaderTop} {HeaderBottom}";
            Seller = seller;
            IsFooterVisible = isFooterVisible;
            CustomerFio = customerFio;
            Properties = properties ?? Array.Empty<OrderPropertyReportData>();
            Products = products ?? Array.Empty<OrderProductReportData>();

            SetPaymentInfo();
        }

        public string DeliveryCost { get; private set; }

        public decimal DeliveryCostDecimal { get; }

        public decimal PrepaymentCostUah { get; }

        public decimal PrepaymentCostUsd { get; }

        public string PrepaymentCost { get; private set; }

        public string Header { get; }

        public string Seller { get; }

        public string HeaderTop { get; }

        public string HeaderBottom { get; }

        public string CustomerFio { get; private set; }

        public int Id { get; }

        public IReadOnlyCollection<OrderProductReportData> Products { get; }

        public IReadOnlyCollection<OrderPropertyReportData> Properties { get; }

        public string Total { get; private set; }

        public string TotalToPay { get; private set; }

        public decimal TotalUahDecimal { get; private set; }

        public decimal TotalUsdDecimal { get; private set; }

        public decimal TotalToPayUahDecimal { get; private set; }

        public decimal TotalToPayUsdDecimal { get; private set; }

        public bool IsFooterVisible { get; }

        private static string GetTotalFormatted(decimal totalUah, decimal totalUsd)
        {
            string totalFormatted = "---";

            if (totalUah > 0 && totalUsd > 0)
            {
                totalFormatted = $"{CurrencyFormatingRules.ToUahStr(totalUah, "C2")}, {CurrencyFormatingRules.ToUsdStr(totalUsd)}";
            }
            else if (totalUsd > 0)
            {
                totalFormatted = CurrencyFormatingRules.ToUsdStr(totalUsd);
            }
            else if (totalUah > 0)
            {
                totalFormatted = CurrencyFormatingRules.ToUahStr(totalUah, "C2");
            }

            return totalFormatted;
        }

        private void SetPaymentInfo()
        {
            DeliveryCost = DeliveryCostDecimal > 0
                ? CurrencyFormatingRules.ToUahStr(DeliveryCostDecimal, "C2")
                : "Безкоштовно";

            TotalUahDecimal = 0m;
            TotalUsdDecimal = 0m;

            foreach (OrderProductReportData x in Products)
            {
                if (x.Currency == Currency.Uah)
                {
                    TotalUahDecimal += x.PriceTotalDecimal;
                }
                else
                {
                    TotalUsdDecimal += x.PriceTotalDecimal;
                }
            }

            TotalToPayUahDecimal = TotalUahDecimal + DeliveryCostDecimal - PrepaymentCostUah;
            TotalToPayUsdDecimal = TotalUsdDecimal - PrepaymentCostUah;

            PrepaymentCost = GetTotalFormatted(PrepaymentCostUah, PrepaymentCostUsd);
            Total = GetTotalFormatted(TotalUahDecimal, TotalUsdDecimal);
            TotalToPay = GetTotalFormatted(TotalToPayUahDecimal, TotalToPayUsdDecimal);
        }
    }
}