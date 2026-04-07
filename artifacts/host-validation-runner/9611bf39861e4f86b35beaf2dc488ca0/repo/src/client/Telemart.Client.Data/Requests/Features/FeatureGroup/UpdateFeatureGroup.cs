using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.FeatureGroup
{
    public class UpdateFeatureGroup : UpdateEntityResultRequestBase<FeatureGroupSimpleDto, FeatureGroupSaveDto>
    {
        public UpdateFeatureGroup(int id, FeatureGroupSaveDto dto)
            : base(dto, ApiResources.FeaturesGroups, id)
        {
        }
    }
}
