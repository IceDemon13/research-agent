using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Contractor.Carry
{
    public sealed class DeleteContractorCarry : DeleteEntityRequestBase
    {
        public DeleteContractorCarry(int contractorId, int carryId)
            : base(ApiResources.Contractors, contractorId.ToString(), "carries", carryId.ToString())
        {
        }
    }
}