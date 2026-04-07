using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Segment
{
    public class SegmentCategorySettingsFeatureViewItem : TelemartViewItemBase, IEquatable<SegmentCategorySettingsFeatureViewItem>
    {
        public SegmentCategorySettingsFeatureViewItem(int id, int categoryId, int featureId, string featureName)
        {
            Id = id;
            CategoryId = categoryId;
            FeatureId = featureId;
            FeatureName = featureName;
        }

        public SegmentCategorySettingsFeatureViewItem()
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
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

        public bool Equals(SegmentCategorySettingsFeatureViewItem other)
        {
            return other != null && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is not null && Equals(obj as SegmentCategorySettingsFeatureViewItem);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}