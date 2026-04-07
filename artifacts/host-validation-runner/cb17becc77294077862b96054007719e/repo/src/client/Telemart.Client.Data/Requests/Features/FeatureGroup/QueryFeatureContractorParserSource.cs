using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.FeatureGroup
{
    public sealed class QueryFeatureContractorParserSource : QueryEntitiesRequestBase<FeatureContractorParserSourceDto>
    {
        public QueryFeatureContractorParserSource(int categoryId)
            : base($"{ApiResources.FeaturesGroups}/feature_contractor_parser_source/{categoryId}")
        {
        }
    }
}