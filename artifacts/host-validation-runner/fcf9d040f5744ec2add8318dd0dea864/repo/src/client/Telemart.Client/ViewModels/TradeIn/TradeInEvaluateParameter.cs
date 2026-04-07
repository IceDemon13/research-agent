using System;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInEvaluateParameter
    {
        public TradeInEvaluateParameter(int id, string brand, string pnModel, decimal? maxPrice, DateTime? dateMaxPrice)
        {
            Id = id;
            Brand = brand;
            PnModel = pnModel;
            MaxPrice = maxPrice;
            DateMaxPrice = dateMaxPrice;
        }

        public int Id { get; }

        public string Brand { get; }

        public string PnModel { get; }

        public decimal? MaxPrice { get; }

        public DateTime? DateMaxPrice { get; }
    }
}