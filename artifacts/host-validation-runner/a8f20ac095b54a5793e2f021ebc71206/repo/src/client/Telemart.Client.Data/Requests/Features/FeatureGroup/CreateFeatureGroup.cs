using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.FeatureGroup
{
    public class CreateFeatureGroup : CreateEntityResultRequestBase<FeatureGroupSimpleDto, FeatureGroupCreateDto>
    {
        public CreateFeatureGroup(FeatureGroupCreateDto dto)
            : base(dto, ApiResources.FeaturesGroups)
        {
        }
    }
}
