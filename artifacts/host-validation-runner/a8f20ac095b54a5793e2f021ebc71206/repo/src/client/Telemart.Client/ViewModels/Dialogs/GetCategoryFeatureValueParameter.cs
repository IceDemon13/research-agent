namespace Telemart.Client.ViewModels.Dialogs
{
    public class GetCategoryFeatureValueParameter
    {
        public GetCategoryFeatureValueParameter(
            bool allRequired,
            bool parentCategoryOnly,
            int? categoryId = null,
            bool parentCategoryFeatures = false,
            int? featureId = null,
            string featureName = null,
            bool multiSelectFeatureValue = false,
            int[] excludedFeatureValueIds = null,
            int? featureValueId = null)
        {
            AllRequired = allRequired;
            ParentCategoryOnly = parentCategoryOnly;
            CategoryId = categoryId;
            FeatureId = featureId;
            FeatureName = featureName;
            ParentCategoryFeatures = parentCategoryFeatures;
            MultiSelectFeatureValue = multiSelectFeatureValue;
            ExcludedFeatureValueIds = excludedFeatureValueIds;
            FeatureValueId = featureValueId;
        }

        public bool AllRequired { get; }

        public bool ParentCategoryOnly { get; }

        public int? CategoryId { get; }

        public int? FeatureId { get; }

        public string FeatureName { get; }

        public bool ParentCategoryFeatures { get; }

        public bool MultiSelectFeatureValue { get; }

        public int[] ExcludedFeatureValueIds { get; }

        public int? FeatureValueId { get; }
    }
}