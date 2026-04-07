using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Actions
{
    public class CreateContractorInBuh1C : CallEntityActionRequestResultBase<ContractorDto>
    {
        public CreateContractorInBuh1C(int id)
            : base(id, ApiResources.Contractors, "create_in_buh1c")
        {
        }
    }
}
