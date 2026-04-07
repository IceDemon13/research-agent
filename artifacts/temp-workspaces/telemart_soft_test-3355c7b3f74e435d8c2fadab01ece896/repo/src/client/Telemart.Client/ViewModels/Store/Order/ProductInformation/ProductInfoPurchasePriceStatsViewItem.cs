using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductInfoPurchasePriceStatsViewItem : BindableBase
    {
        public ProductInfoPurchasePriceStatsViewItem(
            decimal minPriceUsd,
            decimal maxPriceUsd,
            decimal averagePriceUsd,
            decimal minPriceUah,
            decimal maxPriceUah,
            decimal averagePriceUah,
            string label)
        {
            MinPriceUsd = minPriceUsd;
            MaxPriceUsd = maxPriceUsd;
            AveragePriceUsd = averagePriceUsd;
            MinPriceUah = minPriceUah;
            MaxPriceUah = maxPriceUah;
            AveragePriceUah = averagePriceUah;
            Label = label;
        }

        public string Label
        {
            get { return GetProperty(() => Label); }
            set { SetProperty(() => Label, value); }
        }

        public decimal MinPriceUah
        {
            get { return GetProperty(() => MinPriceUah); }
            set { SetProperty(() => MinPriceUah, value); }
        }

        public decimal MaxPriceUah
        {
            get { return GetProperty(() => MaxPriceUah); }
            set { SetProperty(() => MaxPriceUah, value); }
        }

        public decimal AveragePriceUah
        {
            get { return GetProperty(() => AveragePriceUah); }
            set { SetProperty(() => AveragePriceUah, value); }
        }

        public decimal MinPriceUsd
        {
            get { return GetProperty(() => MinPriceUsd); }
            set { SetProperty(() => MinPriceUsd, value); }
        }

        public decimal MaxPriceUsd
        {
            get { return GetProperty(() => MaxPriceUsd); }
            set { SetProperty(() => MaxPriceUsd, value); }
        }

        public decimal AveragePriceUsd
        {
            get { return GetProperty(() => AveragePriceUsd); }
            set { SetProperty(() => AveragePriceUsd, value); }
        }

        public string DisplayMinPrice
        {
            get { return GetProperty(() => DisplayMinPrice); }
            set { SetProperty(() => DisplayMinPrice, value); }
        }

        public string DisplayMaxPrice
        {
            get { return GetProperty(() => DisplayMaxPrice); }
            set { SetProperty(() => DisplayMaxPrice, value); }
        }

        public string DisplayAveragePrice
        {
            get { return GetProperty(() => DisplayAveragePrice); }
            set { SetProperty(() => DisplayAveragePrice, value); }
        }
    }
}