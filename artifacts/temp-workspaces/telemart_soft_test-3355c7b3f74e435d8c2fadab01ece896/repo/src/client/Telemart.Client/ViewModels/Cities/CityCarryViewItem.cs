using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Cities
{
    public class CityCarryViewItem : TelemartCloneableViewItemBase
    {
        public CityCarryViewItem(int carryId, bool availOnWeb, int createdBy)
        {
            CarryId = carryId;
            AvailOnWeb = availOnWeb;
            CreatedBy = createdBy;
        }

        public CityCarryViewItem()
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int CarryId
        {
            get { return GetProperty(() => CarryId); }
            set { SetProperty(() => CarryId, value); }
        }

        public int CreatedBy
        {
            get { return GetProperty(() => CreatedBy); }
            set { SetProperty(() => CreatedBy, value); }
        }

        public bool AvailOnWeb
        {
            get { return GetProperty(() => AvailOnWeb); }
            set { SetProperty(() => AvailOnWeb, value); }
        }
    }
}