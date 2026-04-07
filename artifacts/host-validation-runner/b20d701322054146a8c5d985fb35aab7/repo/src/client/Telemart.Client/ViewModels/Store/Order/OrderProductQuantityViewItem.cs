using DevExpress.Mvvm;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderProductQuantityViewItem : BindableBase
    {
        public int OrderProductId
        {
            get { return GetProperty(() => OrderProductId); }
            set { SetProperty(() => OrderProductId, value); }
        }

        public int Quantity
        {
            get { return GetProperty(() => Quantity); }
            set { SetProperty(() => Quantity, value); }
        }
    }
}
