using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class DeleteOrganizationContact : DeleteEntityRequestBase
    {
        public DeleteOrganizationContact(int organizationId, int contactId)
            : base("organizations", organizationId.ToString(), "contacts", contactId.ToString())
        {
        }
    }
}