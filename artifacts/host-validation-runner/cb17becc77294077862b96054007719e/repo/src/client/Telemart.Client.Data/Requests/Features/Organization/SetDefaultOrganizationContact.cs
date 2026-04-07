using System.Net.Http;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public class SetDefaultOrganizationContact : RestClientGatewayRequestBase<Result<OrganizationContactDto>>
    {
        public SetDefaultOrganizationContact(int organizationId, int contactId)
            : base(HttpMethod.Put)
        {
            PathParameters = new[]
            {
                "organizations",
                organizationId.ToString(),
                "contacts",
                contactId.ToString(),
                "default"
            };
        }
    }
}