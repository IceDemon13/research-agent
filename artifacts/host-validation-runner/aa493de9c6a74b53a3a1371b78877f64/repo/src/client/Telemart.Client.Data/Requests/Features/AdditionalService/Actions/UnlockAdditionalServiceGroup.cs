using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService.Actions
{
    public sealed class UnlockAdditionalServiceGroup : UnlockRequestBase<AdditionalServiceGroupDto>
    {
        public UnlockAdditionalServiceGroup(int id, bool force = false)
            : base(force, ApiResources.AdditionalServicesGroups, id)
        {
        }
    }
}