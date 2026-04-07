using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class ShowcaseCategoryHistoryViewItem : TelemartViewItemBase
    {
        public string Ref
        {
            get { return GetProperty(() => Ref); }
            set { SetProperty(() => Ref, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public string PlaceName
        {
            get { return GetProperty(() => PlaceName); }
            set { SetProperty(() => PlaceName, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int CategoryPlan
        {
            get { return GetProperty(() => CategoryPlan); }
            set { SetProperty(() => CategoryPlan, value); }
        }

        public DateTime ModifiedOn
        {
            get { return GetProperty(() => ModifiedOn); }
            set { SetProperty(() => ModifiedOn, value); }
        }

        public int ModifiedBy
        {
            get { return GetProperty(() => ModifiedBy); }
            set { SetProperty(() => ModifiedBy, value); }
        }
    }
}