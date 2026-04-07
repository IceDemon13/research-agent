using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor
{
    public sealed class UpdateContractorAbcType : UpdateEntityRequestBase<ContractorDto, int>
    {
        public UpdateContractorAbcType(int contractorId, int abcTypeId)
            : base(abcTypeId, ApiResources.Contractors, contractorId, "abc")
        {
        }
    }
}