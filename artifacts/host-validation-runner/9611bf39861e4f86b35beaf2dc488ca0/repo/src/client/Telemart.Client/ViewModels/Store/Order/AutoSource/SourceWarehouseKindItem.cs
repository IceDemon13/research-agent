using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.AutoSource
{
    public sealed class SourceWarehouseKindItem : BindableBase
    {
        public SourceWarehouseKindItem(int warehouseKindId, bool active, int position)
        {
            WarehouseKindId = warehouseKindId;
            Active = active;
            Position = position;
        }

        public int WarehouseKindId
        {
            get { return GetProperty(() => WarehouseKindId); }
            set { SetProperty(() => WarehouseKindId, value); }
        }

        public bool Active
        {
            get { return GetProperty(() => Active); }
            set { SetProperty(() => Active, value); }
        }

        public int Position
        {
            get { return GetProperty(() => Position); }
            set { SetProperty(() => Position, value); }
        }
    }
}