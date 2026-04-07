using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class QueryOrganizationAccounts : QueryEntitiesRequestBase<OrganizationAccountDto>
    {
        public QueryOrganizationAccounts(int organizationId)
            : base(ApiResources.Organizations, organizationId, "accounts")
        {
        }
    }
}