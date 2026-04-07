using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService
{
    public sealed class UpdateAdditionalService : UpdateEntityResultRequestBase<AdditionalServiceDto, AdditionalServiceSaveDto>
    {
        public UpdateAdditionalService(int groupId, AdditionalServiceSaveDto saveDto)
            : base(saveDto, ApiResources.AdditionalServices, groupId)
        {
        }
    }
}