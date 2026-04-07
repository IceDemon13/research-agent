using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Parser.Actions
{
    public class ParseYandexMarketProductFeatures : CallActionWithBodyRequestResultBase<List<YandexMarketProductFeatureDto>, YandexMarketCategoryProductsDto>
    {
        public ParseYandexMarketProductFeatures(YandexMarketCategoryProductsDto productIds)
            : base(productIds, ApiResources.Parser, "parse_ym_category")
        {
        }
    }
}