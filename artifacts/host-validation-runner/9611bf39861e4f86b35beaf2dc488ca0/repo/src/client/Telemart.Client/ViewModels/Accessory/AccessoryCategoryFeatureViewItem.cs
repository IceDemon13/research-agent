using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Accessory
{
    public class AccessoryCategoryFeatureViewItem : TelemartViewItemBase
    {
        public AccessoryCategoryFeatureViewItem(
            int id,
            int accessoryCategoryId,
            int featureId,
            string featureName,
            int featureValueId,
            string featureValueName)
        {
            Id = id;
            AccessoryCategoryId = accessoryCategoryId;
            FeatureId = featureId;
            FeatureName = featureName;
            FeatureValueId = featureValueId;
            FeatureValueName = featureValueName;
        }

        public AccessoryCategoryFeatureViewItem()
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int AccessoryCategoryId
        {
            get { return GetProperty(() => AccessoryCategoryId); }
            set { SetProperty(() => AccessoryCategoryId, value); }
        }

        public int FeatureId
        {
            get { return GetProperty(() => FeatureId); }
            set { SetProperty(() => FeatureId, value); }
        }

        public string FeatureName
        {
            get { return GetProperty(() => FeatureName); }
            set { SetProperty(() => FeatureName, value); }
        }

        public int FeatureValueId
        {
            get { return GetProperty(() => FeatureValueId); }
            set { SetProperty(() => FeatureValueId, value); }
        }

        public string FeatureValueName
        {
            get { return GetProperty(() => FeatureValueName); }
            set { SetProperty(() => FeatureValueName, value); }
        }
    }
}