using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ParserFeature
{
    public class QueryParserFeatureValues : QueryEntitiesRequestBase<ParserFeatureValueDto>
    {
        public QueryParserFeatureValues(IFilteringItem filter)
            : base(filter, $"{ApiResources.Parser}/features/values")
        {
        }
    }
}