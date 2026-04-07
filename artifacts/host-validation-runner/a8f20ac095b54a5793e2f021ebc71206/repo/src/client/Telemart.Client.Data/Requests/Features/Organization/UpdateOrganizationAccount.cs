using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class UpdateOrganizationAccount : UpdateEntityResultRequestBase<OrganizationAccountDto, OrganizationAccountSaveDto>
    {
        public UpdateOrganizationAccount(int organizationId, OrganizationAccountSaveDto dto)
            : base(dto, ApiResources.Organizations, organizationId, "accounts", dto.Id)
        {
        }
    }
}