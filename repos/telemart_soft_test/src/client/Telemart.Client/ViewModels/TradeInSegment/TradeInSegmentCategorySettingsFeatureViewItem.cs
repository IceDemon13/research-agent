using System;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeInSegment
{
    public class TradeInSegmentCategorySettingsFeatureViewItem : TelemartViewItemBase, IEquatable<TradeInSegmentCategorySettingsFeatureViewItem>
    {
        public TradeInSegmentCategorySettingsFeatureViewItem(
            int id,
            int categoryId,
            int featureId,
            string featureName,
            int parentCategoryId,
            string categoryName,
            int categoryEmployeeId,
            bool filledInSegments)
        {
            Id = id;
            CategoryId = categoryId;
            FeatureId = featureId;
            FeatureName = featureName;
            ParentCategoryId = parentCategoryId;
            CategoryName = categoryName;
            CategoryEmployeeId = categoryEmployeeId;
            FilledInSegments = filledInSegments;
        }

        public TradeInSegmentCategorySettingsFeatureViewItem()
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

        public int ParentCategoryId
        {
            get { return GetProperty(() => ParentCategoryId); }
            set { SetProperty(() => ParentCategoryId, value); }
        }

        public string CategoryName
        {
            get { return GetProperty(() => CategoryName); }
            set { SetProperty(() => CategoryName, value); }
        }

        public int CategoryEmployeeId
        {
            get { return GetProperty(() => CategoryEmployeeId); }
            set { SetProperty(() => CategoryEmployeeId, value); }
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

        public bool FilledInSegments
        {
            get { return GetProperty(() => FilledInSegments); }
            set { SetProperty(() => FilledInSegments, value); }
        }

        public bool Equals(TradeInSegmentCategorySettingsFeatureViewItem other)
        {
            return other != null && Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is not null && Equals(obj as TradeInSegmentCategorySettingsFeatureViewItem);
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}