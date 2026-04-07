using DevExpress.Mvvm;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public class PackListOrderViewItem : BindableBase
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

        public OrderStatus OrderState
        {
            get { return GetProperty(() => OrderState); }
            set { SetProperty(() => OrderState, value); }
        }

        public int ProductCount
        {
            get { return GetProperty(() => ProductCount); }
            set { SetProperty(() => ProductCount, value); }
        }

        public int RowCount
        {
            get { return GetProperty(() => RowCount); }
            set { SetProperty(() => RowCount, value); }
        }

        public decimal Weight
        {
            get { return GetProperty(() => Weight); }
            set { SetProperty(() => Weight, value); }
        }
    }
}