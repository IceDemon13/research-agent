using System.Collections.ObjectModel;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public class FeatureGroupViewItem : FeatureGroupSimpleViewItem
    {
        public ObservableCollection<FeatureViewItem> Features
        {
            get { return GetProperty(() => Features); }
            set { SetProperty(() => Features, value); }
        }
    }
}
