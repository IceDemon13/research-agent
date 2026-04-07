using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public class QueryOrganizationContacts : QueryEntitiesPagedRequestBase<OrganizationContactDto>
    {
        public QueryOrganizationContacts(int organizationId)
            : base("organizations", organizationId, "contacts")
        {
        }
    }
}