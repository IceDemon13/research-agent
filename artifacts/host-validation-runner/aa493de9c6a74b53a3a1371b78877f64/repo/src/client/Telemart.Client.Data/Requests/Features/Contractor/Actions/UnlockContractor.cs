using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Actions
{
    public sealed class UnlockContractor : UnlockRequestBase<ContractorDto>
    {
        public UnlockContractor(int id, bool force = false)
            : base(force, ApiResources.Contractors, id)
        {
        }
    }
}
