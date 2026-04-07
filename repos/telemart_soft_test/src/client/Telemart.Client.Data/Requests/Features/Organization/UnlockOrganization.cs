using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Organization
{
    public sealed class UnlockOrganization : UnlockRequestBase<OrganizationDto>
    {
        public UnlockOrganization(int id, bool force = false)
            : base(force, ApiResources.Organizations, id)
        {
        }
    }
}
