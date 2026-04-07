using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class CreateOrganizationContact : CreateEntityResultRequestBase<OrganizationContactDto, OrganizationContactDto>
    {
        public CreateOrganizationContact(int organizationId, OrganizationContactDto dto)
            : base(dto, "organizations", organizationId.ToString(), "contacts")
        {
        }
    }
}