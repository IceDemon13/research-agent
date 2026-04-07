namespace Telemart.Client.ViewModels.Dialogs
{
    public class GetCategoryFeatureParameter
    {
        public GetCategoryFeatureParameter(bool allRequired, bool parentCategoryOnly, int? categoryId = null, bool parentCategoryFeatures = false)
        {
            AllRequired = allRequired;
            ParentCategoryOnly = parentCategoryOnly;
            CategoryId = categoryId;
            ParentCategoryFeatures = parentCategoryFeatures;
        }

        public bool AllRequired { get; }

        public bool ParentCategoryOnly { get; }

        public int? CategoryId { get; }

        public bool ParentCategoryFeatures { get; }
    }
}