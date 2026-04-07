using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Content
{
    public sealed class UpdateFeature : UpdateEntityResultRequestBase<FeatureFullDto, FeatureSaveDto>
    {
        public UpdateFeature(int featureId, FeatureSaveDto dto)
            : base(dto, ApiResources.Features, featureId)
        {
        }
    }
}
