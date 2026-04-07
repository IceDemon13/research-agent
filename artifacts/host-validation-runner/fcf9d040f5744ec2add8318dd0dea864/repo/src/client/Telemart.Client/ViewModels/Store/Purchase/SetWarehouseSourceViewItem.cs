namespace Telemart.Client.ViewModels.Store.Purchase
{
    internal class SetWarehouseSourceViewItem : SetSourceViewItemBase
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
            set { SetProperty(() => WarehouseItems, value, () => { RaisePropertiesChanged(nameof(Available), nameof(SatisfyNeeds)); }); }
        }

        public int ReservedQuantity
        {
            get { return GetProperty(() => ReservedQuantity); }
            set { SetProperty(() => ReservedQuantity, value, () => { RaisePropertiesChanged(nameof(Available), nameof(SatisfyNeeds)); }); }
        }

        public int Needs
        {
            get { return GetProperty(() => Needs); }
            set { SetProperty(() => Needs, value, () => { RaisePropertiesChanged(nameof(Available), nameof(SatisfyNeeds)); }); }
        }

        public int Available => WarehouseItems - ReservedQuantity;

        public bool SatisfyNeeds => Available >= Needs;
    }
}