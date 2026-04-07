using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Content
{
    public sealed class CreateFeature : CreateEntityResultRequestBase<FeatureFullDto, FeatureSaveDto>
    {
        public CreateFeature(FeatureSaveDto dto)
            : base(dto, ApiResources.Features)
        {
        }
    }
}
