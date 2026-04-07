using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Segment
{
    public class SegmentCategoryFeatureValueViewItem : TelemartViewItemBase
    {
        public SegmentCategoryFeatureValueViewItem(int featureValueId, string featureValueName, int segmentCategorySettingsId)
        {
            FeatureValueId = featureValueId;
            FeatureValueName = featureValueName;
            SegmentCategorySettingsId = segmentCategorySettingsId;
        }

        public SegmentCategoryFeatureValueViewItem()
        {
        }

        public int FeatureValueId
        {
            get { return GetProperty(() => FeatureValueId); }
            set { SetProperty(() => FeatureValueId, value); }
        }

        public int SegmentCategorySettingsId
        {
            get { return GetProperty(() => SegmentCategorySettingsId); }
            set { SetProperty(() => SegmentCategorySettingsId, value); }
        }

        public string FeatureValueName
        {
            get { return GetProperty(() => FeatureValueName); }
            set { SetProperty(() => FeatureValueName, value); }
        }
    }
}