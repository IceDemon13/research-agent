using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Contractor.Contact
{
    public sealed class QueryContractorContacts : QueryEntitiesRequestBase<ContractorContactDto>
    {
        public QueryContractorContacts(int contractorId)
            : base(ApiResources.Contractors, contractorId, "contacts")
        {
        }
    }
}