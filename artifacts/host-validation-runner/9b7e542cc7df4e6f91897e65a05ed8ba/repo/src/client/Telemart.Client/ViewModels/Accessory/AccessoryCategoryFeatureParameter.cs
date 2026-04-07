using System.Collections.Generic;
using System.Collections.ObjectModel;
using DevExpress.Mvvm.Native;

namespace Telemart.Client.ViewModels.Accessory
{
    public sealed class AccessoryCategoryFeatureParameter
    {
        public AccessoryCategoryFeatureParameter(int categoryId, string categoryName, int accessoryCategoryId, ICollection<AccessoryCategoryFeatureViewItem> features)
        {
            CategoryId = categoryId;
            Features = features?.ToObservableCollection() ?? new ObservableCollection<AccessoryCategoryFeatureViewItem>();
            AccessoryCategoryId = accessoryCategoryId;
            CategoryName = categoryName;
        }

        public int CategoryId { get; }

        public int AccessoryCategoryId { get; }

        public string CategoryName { get; }

        public ObservableCollection<AccessoryCategoryFeatureViewItem> Features { get; }
    }
}