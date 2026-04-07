using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductHotlineCompetitorViewItem : BindableBase
    {
        public ProductHotlineCompetitorViewItem(string name, int abcId, double? ratio, decimal priceUah, decimal priceUsd)
        {
            Name = name;
            AbcId = abcId;
            Ratio = ratio;
            PriceUah = priceUah;
            PriceUsd = priceUsd;
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int AbcId
        {
            get { return GetProperty(() => AbcId); }
            set { SetProperty(() => AbcId, value); }
        }

        public double? Ratio
        {
            get { return GetProperty(() => Ratio); }
            set { SetProperty(() => Ratio, value); }
        }

        public string DisplayPrice
        {
            get { return GetProperty(() => DisplayPrice); }
            set { SetProperty(() => DisplayPrice, value); }
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
    }
}
