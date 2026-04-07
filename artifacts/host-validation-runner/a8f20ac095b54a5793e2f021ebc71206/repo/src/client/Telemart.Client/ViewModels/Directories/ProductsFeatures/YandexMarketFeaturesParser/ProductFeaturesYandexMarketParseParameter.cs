using System.Collections.Generic;

namespace Telemart.Client.ViewModels.Directories.ProductsFeatures.YandexMarketFeaturesParser
{
    public class ProductFeaturesYandexMarketParseParameter
    {
        public ProductFeaturesYandexMarketParseParameter(
            IReadOnlyCollection<ProductFeaturesYandexMarketProductViewItem> products,
            Dictionary<string, string> featureMap)
        {
            Products = products;
            FeatureMap = featureMap;
        }

        public IReadOnlyCollection<ProductFeaturesYandexMarketProductViewItem> Products { get; }

        public Dictionary<string, string> FeatureMap { get; }
    }
}
