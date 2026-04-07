//using Telemart.Client.Business;

using Telemart.Client.Dictionaries;

namespace Telemart.Client.ReportDesigner
{
    public sealed class OrderProductReportData
    {
        public OrderProductReportData(
            int position,
            int id,
            string name,
            string nameFullUa,
            int quantity,
            decimal price,
            CurrencyType currency,
            string measuringUnit = "шт")
        {
            Position = position;
            Id = id;
            Name = name;
            NameFullUa = nameFullUa;
            Quantity = quantity;
            Currency = currency;
            MeasuringUnit = measuringUnit;
            PriceDecimal = price;

            PriceTotalDecimal = PriceDecimal * Quantity;

            PriceForTape = (int)PriceDecimal;
            TotalForTape = (int)PriceTotalDecimal;

            //if (currency == CurrencyType.Uah)
            //{
            //    Price = CurrencyFormatingRules.ToUahStr(PriceDecimal, "C2");
            //    Total = CurrencyFormatingRules.ToUahStr(PriceTotalDecimal, "C2");
            //}
            //else
            //{
            //    Price = CurrencyFormatingRules.ToUsdStr(PriceDecimal);
            //    Total = CurrencyFormatingRules.ToUsdStr(PriceTotalDecimal);
            //}
        }

        public CurrencyType Currency { get; }

        public string MeasuringUnit { get; set; }

        public string Name { get; }

        public string NameFullUa { get; }

        public int Position { get; }

        public int Id { get; }

        public string Price { get; }

        public int PriceForTape { get; }

        public int Quantity { get; }

        public string Total { get; }

        public int TotalForTape { get; }

        public decimal PriceDecimal { get; }

        public decimal PriceTotalDecimal { get; }
    }
}