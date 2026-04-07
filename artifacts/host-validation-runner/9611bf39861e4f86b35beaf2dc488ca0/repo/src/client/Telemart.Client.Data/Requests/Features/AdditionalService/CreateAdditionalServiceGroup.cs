using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService
{
    public sealed class CreateAdditionalServiceGroup : CreateEntityResultRequestBase<AdditionalServiceGroupDto, AdditionalServiceGroupCreateDto>
    {
        public CreateAdditionalServiceGroup(AdditionalServiceGroupCreateDto dto)
            : base(dto, ApiResources.AdditionalServicesGroups)
        {
        }
    }
}