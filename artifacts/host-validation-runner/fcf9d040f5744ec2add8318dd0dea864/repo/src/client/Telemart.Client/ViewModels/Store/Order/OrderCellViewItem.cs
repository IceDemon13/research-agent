using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderCellViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int CellId
        {
            get { return GetProperty(() => CellId); }
            set { SetProperty(() => CellId, value); }
        }

        public string CellName
        {
            get { return GetProperty(() => CellName); }
            set { SetProperty(() => CellName, value); }
        }

        public int OrderId
        {
            get { return GetProperty(() => OrderId); }
            set { SetProperty(() => OrderId, value); }
        }

        public bool Completed
        {
            get { return GetProperty(() => Completed); }
            set { SetProperty(() => Completed, value); }
        }
    }
}
