using System;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class AutoShowcasesHistoryViewItem : TelemartViewItemBase, ILocalіzableEntity
    {
        public long ShowcaseHistoryId
        {
            get { return GetProperty(() => ShowcaseHistoryId); }
            set { SetProperty(() => ShowcaseHistoryId, value); }
        }

        public int ShowcaseId
        {
            get { return GetProperty(() => ShowcaseId); }
            set { SetProperty(() => ShowcaseId, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string ProductName => this.GetLocalName(LocalizableNameType.Ukr);


        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public int CapacityNew
        {
            get { return GetProperty(() => CapacityNew); }
            set { SetProperty(() => CapacityNew, value); }
        }

        public int CapacityOld
        {
            get { return GetProperty(() => CapacityOld); }
            set { SetProperty(() => CapacityOld, value); }
        }

        public bool ActiveOld
        {
            get { return GetProperty(() => ActiveOld); }
            set { SetProperty(() => ActiveOld, value); }
        }

        public bool ActiveNew
        {
            get { return GetProperty(() => ActiveNew); }
            set { SetProperty(() => ActiveNew, value); }
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

        public string Name { get; set; }

        public string NameUkr { get; set; }

        public string NameEn { get; set; }
    }
}