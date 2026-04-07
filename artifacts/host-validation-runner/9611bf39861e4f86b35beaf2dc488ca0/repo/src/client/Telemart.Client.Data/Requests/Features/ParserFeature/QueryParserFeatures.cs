using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ParserFeature
{
    public class QueryParserFeatures : QueryEntitiesRequestBase<ParserFeatureDto>
    {
        public QueryParserFeatures(IFilteringItem filter)
            : base(filter, $"{ApiResources.Parser}/features")
        {
        }
    }
}