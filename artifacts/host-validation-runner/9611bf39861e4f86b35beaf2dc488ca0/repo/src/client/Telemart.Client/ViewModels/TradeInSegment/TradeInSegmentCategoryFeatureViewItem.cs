using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Cloning;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.TradeInSegment
{
    public class TradeInSegmentCategoryFeatureViewItem : TelemartCloneableViewItemBase
    {
        public TradeInSegmentCategoryFeatureViewItem(
            int id,
            int tradeInSegmentId,
            int tradeInSegmentCategorySettingsId,
            int categoryId,
            int featureId,
            string featureName,
            ObservableCollection<TradeInSegmentCategoryFeatureValueViewItem> featureValues)
        {
            Id = id;
            TradeInSegmentId = tradeInSegmentId;
            TradeInSegmentCategorySettingsId = tradeInSegmentCategorySettingsId;
            CategoryId = categoryId;
            FeatureId = featureId;
            FeatureName = featureName;
            FeatureValues = featureValues;
            DisplayFeatureValues = featureValues.Cast<object>().ToList();
        }

        public TradeInSegmentCategoryFeatureViewItem()
        {
        }

        public int Id
        {
            get { return GetProperty(() => Id); }
            set { SetProperty(() => Id, value); }
        }

        public int TradeInSegmentId
        {
            get { return GetProperty(() => TradeInSegmentId); }
            set { SetProperty(() => TradeInSegmentId, value); }
        }

        public int TradeInSegmentCategorySettingsId
        {
            get { return GetProperty(() => TradeInSegmentCategorySettingsId); }
            set { SetProperty(() => TradeInSegmentCategorySettingsId, value); }
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

        public ObservableCollection<TradeInSegmentCategoryFeatureValueViewItem> FeatureValues
        {
            get { return GetProperty(() => FeatureValues); }
            set { SetProperty(() => FeatureValues, value); }
        }

        public List<object> DisplayFeatureValues
        {
            get { return GetProperty(() => DisplayFeatureValues); }
            set { SetProperty(() => DisplayFeatureValues, value); }
        }

        public static void BuildMetadata(MetadataBuilder<TradeInSegmentCategoryFeatureViewItem> builder)
        {
            builder.Property(x => x.FeatureValues).MatchesInstanceRule((x, y) => x?.Any() == true, () => "Должно быть заполнено минимум 1 значение характеристики");
        }

        public override object Clone()
        {
            TradeInSegmentCategoryFeatureViewItem item = ReflectionObjectCloner.Clone(this);

            item.FeatureValues = FeatureValues?.Select(x =>
            {
                TradeInSegmentCategoryFeatureValueViewItem viewItem = ReflectionObjectCloner.Clone(x);
                return viewItem;
            }).ToObservableCollection();

            item.DisplayFeatureValues = new List<object>(DisplayFeatureValues);

            return item;
        }
    }
}