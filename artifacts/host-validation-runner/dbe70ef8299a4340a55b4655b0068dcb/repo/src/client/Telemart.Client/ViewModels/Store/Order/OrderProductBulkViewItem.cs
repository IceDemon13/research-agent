using DevExpress.Mvvm;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderProductBulkViewItem : BindableBase
    {
        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public ProductDto Product
        {
            get { return GetProperty(() => Product); }
            set { SetProperty(() => Product, value, () => { RaisePropertyChanged(nameof(IsValid)); }); }
        }

        public string ProductDisplayName => Product == null || ProductName.Equals(Product?.Name) ? ProductName : $"{ProductName} ({Product.Name})";

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public bool IsValid => Product != null;
    }
}
