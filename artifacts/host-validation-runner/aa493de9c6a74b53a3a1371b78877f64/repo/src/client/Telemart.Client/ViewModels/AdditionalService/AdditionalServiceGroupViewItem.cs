using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Telemart.Client.ViewModels.AdditionalService
{
    public class AdditionalServiceGroupViewItem : AdditionalServiceGroupSimpleViewItem
    {
        public AdditionalServiceGroupViewItem()
        {
            AdditionalServices = new ObservableCollection<AdditionalServicesViewItem>();
            Groups = new ObservableCollection<AdditionalServiceGroupViewItem>();
        }

        public ObservableCollection<AdditionalServicesViewItem> AdditionalServices
        {
            get
            {
                return GetProperty(() => AdditionalServices);
            }

            set
            {
                value.CollectionChanged += RaiseChildren;
                SetProperty(() => AdditionalServices, value);
            }
        }

        public ObservableCollection<AdditionalServiceGroupViewItem> Groups
        {
            get
            {
                return GetProperty(() => Groups);
            }

            set
            {
                value.CollectionChanged += RaiseChildren;
                SetProperty(() => Groups, value, () => RaisePropertyChanged(nameof(Children)));
            }
        }

        public IEnumerable Children => GetChildren().SelectMany(x => x);

        private IEnumerable<IEnumerable<object>> GetChildren()
        {
            if (AdditionalServices?.Count > 0)
            {
                yield return AdditionalServices;
            }

            if (Groups?.Count > 0)
            {
                yield return Groups;
            }
        }

        private void RaiseChildren(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(Children));
        }
    }
}