using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class UpdateOrganization : UpdateEntityResultRequestBase<OrganizationDto, OrganizationSaveDto>
    {
        public UpdateOrganization(OrganizationSaveDto dto)
            : base(dto, ApiResources.Organizations, dto.Id)
        {
        }
    }
}
