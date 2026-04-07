using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public sealed class ProductSalesHistoryViewItem : BindableBase
    {
        public ProductSalesHistoryViewItem(
            string label,
            DateTime? lastSaleDateTime,
            decimal priceUah,
            decimal priceUsd,
            int quantity,
            bool lastSale = false)
        {
            Label = label;
            LastSaleDateTime = lastSaleDateTime;
            PriceUah = priceUah;
            PriceUsd = priceUsd;
            Quantity = quantity;
            LastSale = lastSale;
        }

        public string Label
        {
            get { return GetProperty(() => Label); }
            private set { SetProperty(() => Label, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            private set { SetProperty(() => Quantity, value); }
        }

        public decimal PriceUah
        {
            get { return GetProperty(() => PriceUah); }
            private set { SetProperty(() => PriceUah, value); }
        }

        public bool LastSale
        {
            get { return GetProperty(() => LastSale); }
            private set { SetProperty(() => LastSale, value); }
        }

        public decimal PriceUsd
        {
            get { return GetProperty(() => PriceUsd); }
            private set { SetProperty(() => PriceUsd, value); }
        }

        public DateTime? LastSaleDateTime
        {
            get { return GetProperty(() => LastSaleDateTime); }
            private set { SetProperty(() => LastSaleDateTime, value); }
        }

        public string DisplayPrice
        {
            get { return GetProperty(() => DisplayPrice); }
            set { SetProperty(() => DisplayPrice, value); }
        }
    }
}
