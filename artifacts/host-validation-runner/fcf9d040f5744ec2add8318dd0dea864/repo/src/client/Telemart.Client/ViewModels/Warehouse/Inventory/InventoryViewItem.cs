using System;
using System.Collections.Generic;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.ViewModels.Warehouse.Inventory
{
    [POCOViewModel(ImplementIDataErrorInfo = true)]
    public class InventoryViewItem
    {
        protected InventoryViewItem()
        {
        }

        public virtual int Id { get; set; }

        public virtual int WarehouseId { get; set; }

        public virtual int? EmployeeLockId { get; set; }

        public virtual string Comment { get; set; }

        public virtual bool TransferScannedBalances { get; set; }

        public virtual int CreatedByEmployeeId { get; set; }

        public virtual DateTime CreatedOn { get; set; }

        public virtual DateTime? InventoriedOn { get; set; }

        public bool IsCompleted => InventoriedOn.HasValue;

        public virtual WarehouseSimpleDto Warehouse { get; set; }

        public virtual EmployeeSimpleDto EmployeeLock { get; set; }

        public virtual string CreatedByEmployeeName { get; set; }

        public virtual List<InventoryProductViewItem> InventoryProducts { get; set; }

        public virtual List<InventoryViewItem> GroupInventories { get; set; }

        public static InventoryViewItem Create()
        {
            return ViewModelSource<InventoryViewItem>.Create();
        }

        protected void OnInventoriedOnChanged(DateTime? oldInventoriedOn)
        {
            this.RaisePropertyChanged(x => x.IsCompleted);
        }
    }
}