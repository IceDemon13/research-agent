using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalService.Actions
{
    public sealed class UnlockAdditionalService : UnlockRequestBase<AdditionalServiceDto>
    {
        public UnlockAdditionalService(int id, bool force = false)
            : base(force, ApiResources.AdditionalServices, id)
        {
        }
    }
}