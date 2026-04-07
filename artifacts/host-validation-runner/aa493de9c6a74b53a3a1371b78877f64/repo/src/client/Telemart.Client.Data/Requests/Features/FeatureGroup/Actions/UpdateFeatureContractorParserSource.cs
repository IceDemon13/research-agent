using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.FeatureGroup.Actions
{
    public sealed class UpdateFeatureContractorParserSource : UpdateEntityRequestBase<Result, FeatureContractorParserSourceSaveDto[]>
    {
        public UpdateFeatureContractorParserSource(FeatureContractorParserSourceSaveDto[] dto)
            : base(dto, ApiResources.FeaturesGroups, "feature_contractor_parser_source")
        {
        }
    }
}