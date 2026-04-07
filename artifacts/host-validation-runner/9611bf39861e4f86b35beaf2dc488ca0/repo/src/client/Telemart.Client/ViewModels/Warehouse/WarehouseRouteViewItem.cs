using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Warehouse
{
    public class WarehouseRouteViewItem : BindableBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int WarehouseFromId
        {
            get { return GetProperty(() => WarehouseFromId); }
            set { SetProperty(() => WarehouseFromId, value); }
        }

        public int WarehouseToId
        {
            get { return GetProperty(() => WarehouseToId); }
            set { SetProperty(() => WarehouseToId, value); }
        }

        public int Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        public WarehouseViewItem WarehouseFrom
        {
            get { return GetProperty(() => WarehouseFrom); }
            set { SetProperty(() => WarehouseFrom, value); }
        }

        public WarehouseViewItem WarehouseTo
        {
            get { return GetProperty(() => WarehouseTo); }
            set { SetProperty(() => WarehouseTo, value); }
        }
    }
}