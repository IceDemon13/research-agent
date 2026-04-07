using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class EditOrderWarrantyViewItem : TelemartCloneableViewItemBase
    {
        public EditOrderWarrantyViewItem(
            int warrantyId,
            int orderProductId,
            string productName,
            int quantity)
        {
            WarrantyId = warrantyId;
            WarrantyOldId = warrantyId;
            OrderProductId = orderProductId;
            ProductName = productName;
            Quantity = quantity;
        }

        public EditOrderWarrantyViewItem()
        {
        }

        public int WarrantyId
        {
            get { return GetProperty(() => WarrantyId); }
            set { SetProperty(() => WarrantyId, value); }
        }

        public int WarrantyOldId
        {
            get { return GetProperty(() => WarrantyOldId); }
            init { SetProperty(() => WarrantyOldId, value); }
        }

        public int OrderProductId
        {
            get { return GetProperty(() => OrderProductId); }
            set { SetProperty(() => OrderProductId, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }
    }
}