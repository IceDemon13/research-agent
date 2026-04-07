using System;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductInfoPriceViewItem : BindableBase
    {
        public ProductInfoPriceViewItem(string name, DateTime modifiedOn, decimal priceUah, decimal priceUsd)
        {
            Name = name;
            ModifiedOn = modifiedOn;
            PriceUah = priceUah;
            PriceUsd = priceUsd;
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public decimal PriceUah
        {
            get { return GetProperty(() => PriceUah); }
            set { SetProperty(() => PriceUah, value); }
        }

        public decimal PriceUsd
        {
            get { return GetProperty(() => PriceUsd); }
            set { SetProperty(() => PriceUsd, value); }
        }

        public string DisplayPrice
        {
            get { return GetProperty(() => DisplayPrice); }
            set { SetProperty(() => DisplayPrice, value); }
        }
    }
}
