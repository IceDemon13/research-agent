using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Contractor.Contact
{
    public sealed class DeleteContractorContact : DeleteEntityRequestBase
    {
        public DeleteContractorContact(int contractorId, int contactId)
            : base(ApiResources.Contractors, contractorId.ToString(), "contacts", contactId.ToString())
        {
        }
    }
}