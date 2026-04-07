using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Contractor.Warehouse
{
    public sealed class DeactivateContractorsWarehouses : CallActionRequestResultBase<object>
    {
        public DeactivateContractorsWarehouses(int contractorId)
            : base(ApiResources.Contractors, $"{contractorId}/deactivate_warehouses")
        {
        }
    }
}