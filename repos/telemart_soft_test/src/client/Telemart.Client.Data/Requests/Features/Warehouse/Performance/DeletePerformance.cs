using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Performance
{
    public class DeletePerformance : DeleteEntityRequestBase
    {
        public DeletePerformance(int warehouseId, int performanceId)
            : base(ApiResources.Warehouses, warehouseId, ApiResources.Perfomances, performanceId)
        {
        }
    }
}