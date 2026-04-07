using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Segment
{
    public class SegmentCategoryFeatureViewItem : TelemartCloneableViewItemBase
    {
        public SegmentCategoryFeatureViewItem(
            int id,
            int segmentId,
            int segmentCategorySettingsId,
            int categoryId,
            int featureId,
            string featureName,
            ObservableCollection<SegmentCategoryFeatureValueViewItem> featureValues)
        {
            Id = id;
            SegmentId = segmentId;
            SegmentCategorySettingsId = segmentCategorySettingsId;
            CategoryId = categoryId;
            FeatureId = featureId;
            FeatureName = featureName;
            FeatureValues = featureValues;
            DisplayFeatureValues = featureValues.Cast<object>().ToList();
        }

        public SegmentCategoryFeatureViewItem()
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int SegmentId
        {
            get { return GetProperty(() => SegmentId); }
            set { SetProperty(() => SegmentId, value); }
        }

        public int SegmentCategorySettingsId
        {
            get { return GetProperty(() => SegmentCategorySettingsId); }
            set { SetProperty(() => SegmentCategorySettingsId, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        public string FeatureName
        {
            get { return GetProperty(() => FeatureName); }
            set { SetProperty(() => FeatureName, value); }
        }

        public int FeatureId
        {
            get { return GetProperty(() => FeatureId); }
            set { SetProperty(() => FeatureId, value); }
        }

        public ObservableCollection<SegmentCategoryFeatureValueViewItem> FeatureValues
        {
            get { return GetProperty(() => FeatureValues); }
            set { SetProperty(() => FeatureValues, value); }
        }

        public List<object> DisplayFeatureValues
        {
            get { return GetProperty(() => DisplayFeatureValues); }
            set { SetProperty(() => DisplayFeatureValues, value); }
        }

        public static void BuildMetadata(MetadataBuilder<SegmentCategoryFeatureViewItem> builder)
        {
            builder.Property(x => x.FeatureValues).MatchesInstanceRule((x, y) => x?.Any() == true, () => "Должно быть заполнено минимум 1 значение характеристики");
        }

        public override object Clone()
        {
            SegmentCategoryFeatureViewItem item = ReflectionObjectCloner.Clone(this);

            item.FeatureValues = FeatureValues?.Select(x =>
            {
                SegmentCategoryFeatureValueViewItem viewItem = ReflectionObjectCloner.Clone(x);
                return viewItem;
            }).ToObservableCollection();

            item.DisplayFeatureValues = new List<object>(DisplayFeatureValues);

            return item;
        }
    }
}