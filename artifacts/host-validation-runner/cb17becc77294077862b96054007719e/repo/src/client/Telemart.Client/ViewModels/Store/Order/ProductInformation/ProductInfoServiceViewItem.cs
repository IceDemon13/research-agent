using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductInfoServiceViewItem : BindableBase
    {
        public ProductInfoServiceViewItem(string name, string tradeInPriceStr)
        {
            Name = name;
            TradeInPriceStr = tradeInPriceStr;
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string TradeInPriceStr
        {
            get { return GetProperty(() => TradeInPriceStr); }
            set { SetProperty(() => TradeInPriceStr, value); }
        }
    }
}