using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public sealed class UnlockWarehouse : UnlockRequestBase<WarehouseDto>
    {
        public UnlockWarehouse(int id, bool force = false)
            : base(force, ApiResources.Warehouses, id)
        {
        }
    }
}