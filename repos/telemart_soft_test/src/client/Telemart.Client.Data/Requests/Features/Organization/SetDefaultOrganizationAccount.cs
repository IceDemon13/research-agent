using System.Net.Http;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class SetDefaultOrganizationAccount : RestClientGatewayRequestBase<Result<OrganizationAccountDto>>
    {
        public SetDefaultOrganizationAccount(int organizationId, int accountId)
            : base(HttpMethod.Put)
        {
            PathParameters = new[] { "organizations", organizationId.ToString(), "accounts", accountId.ToString(), "default" };
        }
    }
}