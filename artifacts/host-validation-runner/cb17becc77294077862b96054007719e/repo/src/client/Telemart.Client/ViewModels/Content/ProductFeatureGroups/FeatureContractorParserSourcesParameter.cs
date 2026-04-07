namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class FeatureContractorParserSourcesParameter
    {
        public FeatureContractorParserSourcesParameter(int categoryId, string categoryName)
        {
            CategoryId = categoryId;
            CategoryName = categoryName;
        }

        public int CategoryId { get; }

        public string CategoryName { get; }
    }
}