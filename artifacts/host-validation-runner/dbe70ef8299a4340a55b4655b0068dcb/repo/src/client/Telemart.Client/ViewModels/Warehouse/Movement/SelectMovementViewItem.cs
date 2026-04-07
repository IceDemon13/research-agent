using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public class SelectMovementViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            init { SetProperty(() => Id, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            init { SetProperty(() => Name, value); }
        }

        public string ToLocationName
        {
            get { return GetProperty(() => ToLocationName); }
            init { SetProperty(() => ToLocationName, value); }
        }
    }
}