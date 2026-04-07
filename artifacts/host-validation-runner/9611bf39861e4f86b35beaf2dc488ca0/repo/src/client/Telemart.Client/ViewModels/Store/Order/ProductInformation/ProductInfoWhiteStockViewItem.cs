using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductInfoWhiteStockViewItem : BindableBase
    {
        public string OrganizationName
        {
            get { return GetProperty(() => OrganizationName); }
            set { SetProperty(() => OrganizationName, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int QuantityFree
        {
            get { return GetProperty(() => QuantityFree); }
            set { SetProperty(() => QuantityFree, value); }
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