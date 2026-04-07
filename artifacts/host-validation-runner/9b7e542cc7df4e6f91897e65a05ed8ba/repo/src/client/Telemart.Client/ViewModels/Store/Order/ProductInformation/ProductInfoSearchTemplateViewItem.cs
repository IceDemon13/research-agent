using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductInfoSearchTemplateViewItem : BindableBase
    {
        public int SearchTemplateId
        {
            get { return GetProperty(() => SearchTemplateId); }
            set { SetProperty(() => SearchTemplateId, value); }
        }

        public string SearchTemplateName
        {
            get { return GetProperty(() => SearchTemplateName); }
            set { SetProperty(() => SearchTemplateName, value); }
        }

        public decimal MinPriceUah
        {
            get { return GetProperty(() => MinPriceUah); }
            set { SetProperty(() => MinPriceUah, value); }
        }

        public decimal MinPriceUsd
        {
            get { return GetProperty(() => MinPriceUsd); }
            set { SetProperty(() => MinPriceUsd, value); }
        }

        public string DisplayMinPrice
        {
            get { return GetProperty(() => DisplayMinPrice); }
            set { SetProperty(() => DisplayMinPrice, value); }
        }

        public decimal AvgPriceUah
        {
            get { return GetProperty(() => AvgPriceUah); }
            set { SetProperty(() => AvgPriceUah, value); }
        }

        public decimal AvgPriceUsd
        {
            get { return GetProperty(() => AvgPriceUsd); }
            set { SetProperty(() => AvgPriceUsd, value); }
        }

        public string DisplayAvgPrice
        {
            get { return GetProperty(() => DisplayAvgPrice); }
            set { SetProperty(() => DisplayAvgPrice, value); }
        }

        public decimal MaxPriceUah
        {
            get { return GetProperty(() => MaxPriceUah); }
            set { SetProperty(() => MaxPriceUah, value); }
        }

        public decimal MaxPriceUsd
        {
            get { return GetProperty(() => MaxPriceUsd); }
            set { SetProperty(() => MaxPriceUsd, value); }
        }

        public string DisplayMaxPrice
        {
            get { return GetProperty(() => DisplayMaxPrice); }
            set { SetProperty(() => DisplayMaxPrice, value); }
        }
    }
}