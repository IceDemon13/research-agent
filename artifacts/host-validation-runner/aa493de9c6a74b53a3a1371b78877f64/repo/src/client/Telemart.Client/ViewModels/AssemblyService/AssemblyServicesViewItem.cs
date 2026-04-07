using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Common;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public class AssemblyServicesViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public int? OrderProductId
        {
            get { return GetProperty(() => OrderProductId); }
            set { SetProperty(() => OrderProductId, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int OrderStateId
        {
            get { return GetProperty(() => OrderStateId); }
            set { SetProperty(() => OrderStateId, value); }
        }

        public int? Places
        {
            get { return GetProperty(() => Places); }
            set { SetProperty(() => Places, value); }
        }

        public string NomenclatureSeries
        {
            get { return GetProperty(() => NomenclatureSeries); }
            set { SetProperty(() => NomenclatureSeries, value); }
        }

        public int? OrderWarehouseId
        {
            get { return GetProperty(() => OrderWarehouseId); }
            set { SetProperty(() => OrderWarehouseId, value); }
        }

        public string OrderComment
        {
            get { return GetProperty(() => OrderComment); }
            set { SetProperty(() => OrderComment, value); }
        }

        public DateTime? OrderDeliveryTime
        {
            get { return GetProperty(() => OrderDeliveryTime); }
            set { SetProperty(() => OrderDeliveryTime, value); }
        }

        public DateTime? OrderDeliveryTimeTo
        {
            get { return GetProperty(() => OrderDeliveryTimeTo); }
            set { SetProperty(() => OrderDeliveryTimeTo, value); }
        }

        public int StateId
        {
            get { return GetProperty(() => StateId); }
            set { SetProperty(() => StateId, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int EmployeeId
        {
            get { return GetProperty(() => EmployeeId); }
            set { SetProperty(() => EmployeeId, value); }
        }

        public int ProductsCount
        {
            get { return GetProperty(() => ProductsCount); }
            set { SetProperty(() => ProductsCount, value); }
        }

        public Subdivision Subdivision
        {
            get { return GetProperty(() => Subdivision); }
            set { SetProperty(() => Subdivision, value); }
        }

        public string EmployeeLockName
        {
            get { return GetProperty(() => EmployeeLockName); }
            set { SetProperty(() => EmployeeLockName, value); }
        }

        public int? EmployeeLockId
        {
            get { return GetProperty(() => EmployeeLockId); }
            set { SetProperty(() => EmployeeLockId, value); }
        }

        public DateTime AssemblyDate
        {
            get { return GetProperty(() => AssemblyDate); }
            set { SetProperty(() => AssemblyDate, value); }
        }

        public DateTime? ArrivedOn
        {
            get { return GetProperty(() => ArrivedOn); }
            set { SetProperty(() => ArrivedOn, value); }
        }

        public DateTime? AssembledOn
        {
            get { return GetProperty(() => AssembledOn); }
            set { SetProperty(() => AssembledOn, value); }
        }

        public int? AssembledBy
        {
            get { return GetProperty(() => AssembledBy); }
            set { SetProperty(() => AssembledBy, value); }
        }

        public DateTime? CompletedOn
        {
            get { return GetProperty(() => CompletedOn); }
            set { SetProperty(() => CompletedOn, value); }
        }

        public int? ParentAssemblyServiceId
        {
            get { return GetProperty(() => ParentAssemblyServiceId); }
            set { SetProperty(() => ParentAssemblyServiceId, value); }
        }

        public List<AssemblyServiceProductViewItem> Products
        {
            get { return GetProperty(() => Products); }
            set { SetProperty(() => Products, value); }
        }

        public List<AdditionalServiceProductDto> AdditionalServices
        {
            get { return GetProperty(() => AdditionalServices); }
            set { SetProperty(() => AdditionalServices, value); }
        }

        public string KindOfJob => ParentAssemblyServiceId.HasValue ? "Разборка" : "Сборка";

        public bool NotCompletedStateAndHasQuickAssemblyAdditionalService => StateId != AssemblyServiceState.Completed.Id && AdditionalServices?.Any(x => x.AdditionalServiceProductId == Constants.QuickAssemblyServiceProductId) == true;
    }
}