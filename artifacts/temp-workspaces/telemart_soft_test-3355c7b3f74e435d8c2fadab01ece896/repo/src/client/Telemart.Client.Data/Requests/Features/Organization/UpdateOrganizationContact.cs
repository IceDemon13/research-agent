using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class UpdateOrganizationContact : UpdateEntityResultRequestBase<OrganizationContactDto, OrganizationContactDto>
    {
        public UpdateOrganizationContact(int organizationId, OrganizationContactDto dto)
            : base(dto, ApiResources.Organizations, organizationId, "contacts", dto.Id)
        {
        }
    }
}