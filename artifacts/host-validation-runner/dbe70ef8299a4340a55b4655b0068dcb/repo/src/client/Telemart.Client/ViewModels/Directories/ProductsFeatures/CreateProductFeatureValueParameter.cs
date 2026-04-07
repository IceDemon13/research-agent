namespace Telemart.Client.ViewModels.Directories.ProductsFeatures
{
    public class CreateProductFeatureValueParameter
    {
        public CreateProductFeatureValueParameter(int featureId, string regex, bool multiLanguage, bool isUrlReadOnly)
        {
            FeatureId = featureId;
            Regex = regex;
            MultiLanguage = multiLanguage;
            IsUrlReadOnly = isUrlReadOnly;
        }

        public int FeatureId { get; }

        public bool MultiLanguage { get; }

        public string Regex { get; }

        public bool IsUrlReadOnly { get; }
    }
}
