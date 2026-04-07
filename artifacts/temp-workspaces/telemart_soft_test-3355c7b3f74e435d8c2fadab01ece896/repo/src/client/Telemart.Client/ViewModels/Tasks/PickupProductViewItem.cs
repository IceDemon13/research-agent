using System.Collections.Generic;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.ViewModels.Tasks
{
    public class PickupProductViewItem : TelemartCloneableViewItemBase, ILocalіzableEntity
    {
        public PickupProductViewItem()
        {
            ReasonIds = new List<object>();
        }

        public int ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value, () => RaisePropertyChanged(nameof(ProductName))); }
        }

        public List<object> ReasonIds
        {
            get { return GetProperty(() => ReasonIds); }
            set { SetProperty(() => ReasonIds, value); }
        }

        public bool ReturnOnMainWarehouse
        {
            get { return GetProperty(() => ReturnOnMainWarehouse); }
            set
            {
                SetProperty(() => ReturnOnMainWarehouse, value, () =>
                {
                    if (ReturnOnMainWarehouse && KeepOnPickup)
                    {
                        KeepOnPickup = false;
                    }
                });
            }
        }

        public bool KeepOnPickup
        {
            get { return GetProperty(() => KeepOnPickup); }
            set
            {
                SetProperty(() => KeepOnPickup, value, () =>
                {
                    if (KeepOnPickup && ReturnOnMainWarehouse)
                    {
                        ReturnOnMainWarehouse = false;
                    }
                });
            }
        }

        public bool Processed
        {
            get { return GetProperty(() => Processed); }
            set { SetProperty(() => Processed, value); }
        }

        public string ProductName => this.GetLocalName(LocalizableNameType.Ukr);
    }
}