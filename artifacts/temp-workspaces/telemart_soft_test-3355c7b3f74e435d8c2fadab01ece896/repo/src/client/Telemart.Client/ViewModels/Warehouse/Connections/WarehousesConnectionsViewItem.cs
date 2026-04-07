using System;
using System.Collections.ObjectModel;
using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Warehouse.Connections
{
    public class WarehousesConnectionsViewItem : BindableBase
    {
        public int FromWarehouseId
        {
            get { return GetProperty(() => FromWarehouseId); }
            set { SetProperty(() => FromWarehouseId, value); }
        }

        public int ToWarehouseId
        {
            get { return GetProperty(() => ToWarehouseId); }
            set { SetProperty(() => ToWarehouseId, value); }
        }

        public int RouteId
        {
            get { return GetProperty(() => RouteId); }
            set { SetProperty(() => RouteId, value); }
        }

        public int RouteTimeId
        {
            get { return GetProperty(() => RouteTimeId); }
            set { SetProperty(() => RouteTimeId, value); }
        }

        public DateTime StartDate
        {
            get { return GetProperty(() => StartDate); }
            set { SetProperty(() => StartDate, value); }
        }

        public DateTime EndDate
        {
            get { return GetProperty(() => EndDate); }
            set { SetProperty(() => EndDate, value); }
        }

        public int Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }

        public ReadOnlyObservableCollection<WarehousesConnectionsViewItem> Connections
        {
            get { return GetProperty(() => Connections); }
            set { SetProperty(() => Connections, value); }
        }
    }
}
