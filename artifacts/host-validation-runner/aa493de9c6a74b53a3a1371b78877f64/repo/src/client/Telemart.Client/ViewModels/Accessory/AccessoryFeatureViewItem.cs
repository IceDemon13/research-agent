using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Accessory
{
    public class AccessoryFeatureViewItem : TelemartViewItemBase
    {
        public AccessoryFeatureViewItem(
            int id,
            int featureId,
            int featureValueId,
            int accessoryId,
            string featureName,
            string featureValueName)
        {
            Id = id;
            FeatureId = featureId;
            FeatureValueId = featureValueId;
            AccessoryId = accessoryId;
            FeatureName = featureName;
            FeatureValueName = featureValueName;
        }

        public AccessoryFeatureViewItem()
        {
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