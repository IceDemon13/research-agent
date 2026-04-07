using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Warehouse;

namespace Telemart.Client.Data.Requests.Features.Warehouse
{
    public sealed class QueryWarehouseAllowedEmployees : QueryEntitiesRequestBase<WarehouseAllowedEmployeesDto>
    {
        public QueryWarehouseAllowedEmployees()
            : base($"{ApiResources.Warehouses}/allowed_employees")
        {
        }
    }
}