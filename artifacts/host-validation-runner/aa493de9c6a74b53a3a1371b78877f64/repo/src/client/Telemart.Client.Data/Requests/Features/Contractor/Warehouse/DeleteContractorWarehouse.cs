using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Contractor.Warehouse
{
    public sealed class DeleteContractorWarehouse : DeleteEntityRequestBase
    {
        public DeleteContractorWarehouse(int contractorId, int entityId)
            : base(ApiResources.Contractors, contractorId.ToString(), "warehouses", entityId.ToString())
        {
        }
    }
}