using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductLeftoverViewItem : BindableBase
    {
        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public string WarehouseName
        {
            get { return GetProperty(() => WarehouseName); }
            set { SetProperty(() => WarehouseName, value); }
        }

        public int WarehousePosition
        {
            get { return GetProperty(() => WarehousePosition); }
            set { SetProperty(() => WarehousePosition, value); }
        }

        public int WarehouseItems
        {
            get { return GetProperty(() => WarehouseItems); }
            set { SetProperty(() => WarehouseItems, value, () => RaisePropertyChanged(nameof(Available))); }
        }

        public int ReservedQuantity
        {
            get { return GetProperty(() => ReservedQuantity); }
            set { SetProperty(() => ReservedQuantity, value, () => RaisePropertyChanged(nameof(Available))); }
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

        public int Available => WarehouseItems - ReservedQuantity;
    }
}
