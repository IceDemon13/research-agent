using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class QueryOrganization : QueryEntityRequestBase<OrganizationDto>
    {
        public QueryOrganization(int id)
            : base(ApiResources.Organizations, id)
        {
        }
    }
}