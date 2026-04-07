using Telemart.Client.Dictionaries;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse.Movement
{
    public class MovementProductSnViewItem : TelemartViewItemBase
    {
        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public string Sn
        {
            get { return GetProperty(() => Sn); }
            set { SetProperty(() => Sn, value); }
        }

        public string NomenclatureSeries
        {
            get { return GetProperty(() => NomenclatureSeries); }
            set { SetProperty(() => NomenclatureSeries, value); }
        }

        public bool ScannedIn
        {
            get { return GetProperty(() => ScannedIn); }
            set { SetProperty(() => ScannedIn, value); }
        }

        public bool ScannedOut
        {
            get { return GetProperty(() => ScannedOut); }
            set { SetProperty(() => ScannedOut, value); }
        }

        public bool NotSaved
        {
            get { return GetProperty(() => NotSaved); }
            set { SetProperty(() => NotSaved, value); }
        }

        public void Scan(int movementStateId)
        {
            if (movementStateId == MovementState.New.Id)
            {
                ScannedOut = true;
            }
            else if (movementStateId == MovementState.Arrived.Id)
            {
                ScannedIn = true;
            }
        }
    }
}