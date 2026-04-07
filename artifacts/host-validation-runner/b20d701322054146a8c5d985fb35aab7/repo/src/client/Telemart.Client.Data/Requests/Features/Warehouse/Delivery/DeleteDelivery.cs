using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Delivery
{
    public class DeleteDelivery : DeleteEntityRequestBase
    {
        public DeleteDelivery(int warehouseId, int deliveryId)
            : base(ApiResources.Warehouses, warehouseId.ToString(), ApiResources.Deliveries, deliveryId.ToString())
        {
        }
    }
}