using System;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    internal sealed class SetMovementSourceViewItem : SetSourceViewItemBase
    {
        public int MovementId
        {
            get { return GetProperty(() => MovementId); }
            set { SetProperty(() => MovementId, value); }
        }

        public int WarehouseFromId
        {
            get { return GetProperty(() => WarehouseFromId); }
            set { SetProperty(() => WarehouseFromId, value); }
        }

        public string WarehouseFromName
        {
            get { return GetProperty(() => WarehouseFromName); }
            set { SetProperty(() => WarehouseFromName, value); }
        }

        public int WarehouseToId
        {
            get { return GetProperty(() => WarehouseToId); }
            set { SetProperty(() => WarehouseToId, value); }
        }

        public string WarehouseToName
        {
            get { return GetProperty(() => WarehouseToName); }
            set { SetProperty(() => WarehouseToName, value); }
        }

        public DateTime DateOut
        {
            get { return GetProperty(() => DateOut); }
            set { SetProperty(() => DateOut, value); }
        }

        public DateTime DateIn
        {
            get { return GetProperty(() => DateIn); }
            set { SetProperty(() => DateIn, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }

        public int AvailableQuantity
        {
            get { return GetProperty(() => AvailableQuantity); }
            set { SetProperty(() => AvailableQuantity, value); }
        }

        public int Needs
        {
            get { return GetProperty(() => Needs); }
            set { SetProperty(() => Needs, value, () => { RaisePropertiesChanged(nameof(AvailableQuantity), nameof(SatisfyNeeds)); }); }
        }

        public bool SatisfyNeeds => AvailableQuantity >= Needs;
    }
}