using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService
{
    public sealed class CreateAdditionalService : CreateEntityResultRequestBase<AdditionalServiceDto, AdditionalServiceCreateDto>
    {
        public CreateAdditionalService(AdditionalServiceCreateDto dto)
            : base(dto, ApiResources.AdditionalServices)
        {
        }
    }
}