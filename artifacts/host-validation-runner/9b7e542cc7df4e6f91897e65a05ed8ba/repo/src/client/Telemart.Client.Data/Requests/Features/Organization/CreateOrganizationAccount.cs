using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class CreateOrganizationAccount : CreateEntityResultRequestBase<OrganizationAccountDto, OrganizationAccountDto>
    {
        public CreateOrganizationAccount(int organizationId, OrganizationAccountDto dto)
            : base(dto, ApiResources.Organizations, organizationId, "accounts")
        {
        }
    }
}