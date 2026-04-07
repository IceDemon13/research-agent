using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups.FeatureValues
{
    public class FeatureValuesParameter
    {
        public FeatureValuesParameter(int categoryId, IReadOnlyCollection<FeatureValueViewItem> featureValues, int? featureId = null)
        {
            CategoryId = categoryId;
            FeatureId = featureId;
            FeatureValues = featureValues;
        }

        public int? FeatureId { get; }

        public int CategoryId { get; }

        public IReadOnlyCollection<FeatureValueViewItem> FeatureValues { get; }
    }
}
