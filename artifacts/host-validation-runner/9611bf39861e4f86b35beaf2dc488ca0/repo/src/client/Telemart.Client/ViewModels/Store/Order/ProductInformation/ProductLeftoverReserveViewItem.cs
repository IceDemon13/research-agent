using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order.ProductInformation
{
    public class ProductLeftoverReserveViewItem : BindableBase
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

        public int OrderReserveQuantity
        {
            get { return GetProperty(() => OrderReserveQuantity); }
            set { SetProperty(() => OrderReserveQuantity, value); }
        }

        public int ReturnInvoiceReserveQuantity
        {
            get { return GetProperty(() => ReturnInvoiceReserveQuantity); }
            set { SetProperty(() => ReturnInvoiceReserveQuantity, value); }
        }

        public int AssembledComputerRuleReserveQuantity
        {
            get { return GetProperty(() => AssembledComputerRuleReserveQuantity); }
            set { SetProperty(() => AssembledComputerRuleReserveQuantity, value); }
        }

        public int ReservedByMovement
        {
            get { return GetProperty(() => ReservedByMovement); }
            set { SetProperty(() => ReservedByMovement, value); }
        }
    }
}