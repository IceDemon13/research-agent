using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class QueryOrganizations : QueryEntitiesPagedRequestBase<OrganizationDto>
    {
        public QueryOrganizations()
            : base(null, ApiResources.Organizations)
        {
        }
    }
}
