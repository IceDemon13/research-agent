using System;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Purchase
{
    public class SetInvoiceSourceViewItem : SetSourceViewItemBase
    {
        protected SetInvoiceSourceViewItem()
        {
        }

        public int? InvoiceId
        {
            get { return GetProperty(() => InvoiceId); }
            set { SetProperty(() => InvoiceId, value, () => { RaisePropertyChanged(nameof(InvoiceExists)); }); }
        }

        public InvoiceState State
        {
            get { return GetProperty(() => State); }
            set { SetProperty(() => State, value, () => { RaisePropertyChanged(nameof(SuccedsByQuantity)); }); }
        }

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            set { SetProperty(() => CarryType, value); }
        }

        public int? SupplierWarehouseId
        {
            get { return GetProperty(() => SupplierWarehouseId); }
            set { SetProperty(() => SupplierWarehouseId, value); }
        }

        public string SupplierWarehouseName
        {
            get { return GetProperty(() => SupplierWarehouseName); }
            set { SetProperty(() => SupplierWarehouseName, value); }
        }

        public int PaymentId
        {
            get { return GetProperty(() => PaymentId); }
            set { SetProperty(() => PaymentId, value); }
        }

        public DateTime? DateClose
        {
            get { return GetProperty(() => DateClose); }
            set { SetProperty(() => DateClose, value); }
        }

        public DateTime? DateGet
        {
            get { return GetProperty(() => DateGet); }
            set { SetProperty(() => DateGet, value); }
        }

        public DateTime? DateArrive
        {
            get { return GetProperty(() => DateArrive); }
            set { SetProperty(() => DateArrive, value); }
        }

        public int SupplierId
        {
            get { return GetProperty(() => SupplierId); }
            set { SetProperty(() => SupplierId, value); }
        }

        public int WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int TargetWarehouseId
        {
            get { return GetProperty(() => TargetWarehouseId); }
            set { SetProperty(() => TargetWarehouseId, value); }
        }

        public int OrderQuantity
        {
            get { return GetProperty(() => OrderQuantity); }
            set { SetProperty(() => OrderQuantity, value, () => { RaisePropertyChanged(nameof(SuccedsByQuantity)); }); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int? AvailableQuantity
        {
            get { return GetProperty(() => AvailableQuantity); }
            set { SetProperty(() => AvailableQuantity, value, () => { RaisePropertyChanged(nameof(SuccedsByQuantity)); }); }
        }

        public bool IsNewRow
        {
            get { return GetProperty(() => IsNewRow); }
            set { SetProperty(() => IsNewRow, value); }
        }

        public bool InvoiceExists => InvoiceId.HasValue;

        public bool SuccedsByQuantity => (State != InvoiceState.Closed && State != InvoiceState.Arrived) || OrderQuantity <= AvailableQuantity;

        public static SetInvoiceSourceViewItem Create(DateTime? orderDeliveryDateTime, int orderQuantity, OrderStatus orderState)
        {
            SetInvoiceSourceViewItem invoiceSourceViewItem = new SetInvoiceSourceViewItem
            {
                OrderDeliveryDateTime = orderDeliveryDateTime,
                OrderQuantity = orderQuantity,
                OrderState = orderState,
                IsNewRow = false
            };
            return invoiceSourceViewItem;
        }

        public static SetInvoiceSourceViewItem Create()
        {
            SetInvoiceSourceViewItem invoiceSourceViewItem = new SetInvoiceSourceViewItem
            {
                OrderDeliveryDateTime = null,
                OrderQuantity = 0,
                OrderState = null,
                IsNewRow = true
            };
            return invoiceSourceViewItem;
        }
    }
}
