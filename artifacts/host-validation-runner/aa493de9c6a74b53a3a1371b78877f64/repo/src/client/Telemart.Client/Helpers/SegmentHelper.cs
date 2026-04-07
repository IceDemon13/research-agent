using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm.Native;
using Telemart.Client.TransferObjects.Segment;
using Telemart.Client.TransferObjects.TradeInSegment;
using Telemart.Client.ViewModels.Segment;
using Telemart.Client.ViewModels.TradeInSegment;

namespace Telemart.Client.Helpers
{
    public static class SegmentHelper
    {
        public static IEnumerable<SegmentCategoryFeatureViewItem> GetSegmentCategoryFeatures(this SegmentDto dto)
        {
            return dto.CategoryFeatures
                .OrderBy(f => f.FeatureName)
                .GroupBy(s => s.SegmentCategorySettingsId)
                .Select(p => new { First = p.First(), Group = p })
                .Select(q => new SegmentCategoryFeatureViewItem(
                    q.First.Id,
                    q.First.SegmentId,
                    q.First.SegmentCategorySettingsId,
                    q.First.CategoryId,
                    q.First.FeatureId,
                    q.First.FeatureName,
                    q.Group.Select(t => new SegmentCategoryFeatureValueViewItem(t.FeatureValueId, t.FeatureValueName, t.SegmentCategorySettingsId))
                        .OrderBy(g => g.FeatureValueName)
                        .ToObservableCollection()));
        }

        public static string GetSegmentFeaturesString(this IEnumerable<SegmentCategoryFeatureViewItem> categoryFeatures, bool splitByNewLine = false)
        {
            return $"⚙ {string.Join(" ⚙ ", categoryFeatures.Select(x => $"{x.FeatureName}: {string.Join(',', x.FeatureValues.Select(z => z.FeatureValueName))}{(splitByNewLine ? Environment.NewLine : string.Empty)}"))}";
        }

        public static IEnumerable<TradeInSegmentCategoryFeatureViewItem> GetSegmentCategoryFeatures(this TradeInSegmentDto dto)
        {
            return dto.TradeInSegmentCategoryFeatures
                .OrderBy(f => f.FeatureName)
                .GroupBy(s => s.TradeInSegmentCategorySettingsId)
                .Select(p => new { First = p.First(), Group = p })
                .Select(q => new TradeInSegmentCategoryFeatureViewItem(
                    q.First.Id,
                    q.First.TradeInSegmentId,
                    q.First.TradeInSegmentCategorySettingsId,
                    q.First.CategoryId,
                    q.First.FeatureId,
                    q.First.FeatureName,
                    q.Group.Select(t => new TradeInSegmentCategoryFeatureValueViewItem(t.FeatureValueId.Value, t.FeatureValueName, t.TradeInSegmentCategorySettingsId))
                        .OrderBy(g => g.FeatureValueName)
                        .ToObservableCollection()));
        }

        public static string GetSegmentFeaturesString(this IEnumerable<TradeInSegmentCategoryFeatureViewItem> categoryFeatures, bool splitByNewLine = false)
        {
            return $"⚙ {string.Join(" ⚙ ", categoryFeatures.Select(x => $"{x.FeatureName}: {string.Join(',', x.FeatureValues.Select(z => z.FeatureValueName))}{(splitByNewLine ? Environment.NewLine : string.Empty)}"))}";
        }
    }
}