using System.Collections.ObjectModel;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Accessory
{
    public class AccessoryCategoryViewItem : TelemartViewItemBase
    {
        public AccessoryCategoryViewItem(int categoryId, string categoryName)
            : this()
        {
            CategoryId = categoryId;
            CategoryName = categoryName;
        }

        public AccessoryCategoryViewItem()
        {
            Features = new();
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int AccessoryId
        {
            get { return GetProperty(() => AccessoryId); }
            set { SetProperty(() => AccessoryId, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public int FeaturesQuantity => Features?.Count ?? 0;

        public ObservableCollection<AccessoryCategoryFeatureViewItem> Features
        {
            get { return GetProperty(() => Features); }
            set { SetProperty(() => Features, value, () => RaisePropertyChanged(nameof(FeaturesQuantity))); }
        }
    }
}