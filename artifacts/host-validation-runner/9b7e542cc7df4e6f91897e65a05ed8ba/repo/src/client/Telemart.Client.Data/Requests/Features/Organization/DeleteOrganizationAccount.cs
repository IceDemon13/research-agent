using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class DeleteOrganizationAccount : DeleteEntityRequestBase
    {
        public DeleteOrganizationAccount(int organizationId, int accountId)
            : base("organizations", organizationId.ToString(), "accounts", accountId.ToString())
        {
        }
    }
}