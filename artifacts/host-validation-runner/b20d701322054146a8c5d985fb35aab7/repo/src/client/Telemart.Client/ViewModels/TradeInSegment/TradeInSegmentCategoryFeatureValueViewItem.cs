using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeInSegment
{
    public class TradeInSegmentCategoryFeatureValueViewItem : TelemartViewItemBase
    {
        public TradeInSegmentCategoryFeatureValueViewItem(int featureValueId, string featureValueName, int segmentCategorySettingsId)
        {
            FeatureValueId = featureValueId;
            FeatureValueName = featureValueName;
            SegmentCategorySettingsId = segmentCategorySettingsId;
        }

        public TradeInSegmentCategoryFeatureValueViewItem()
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