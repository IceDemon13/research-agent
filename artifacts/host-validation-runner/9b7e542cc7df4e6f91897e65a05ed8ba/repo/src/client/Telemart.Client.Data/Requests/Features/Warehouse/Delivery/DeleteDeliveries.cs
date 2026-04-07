using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Warehouse.Delivery;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Delivery
{
    public sealed class DeleteDeliveries : CallActionWithBodyRequestResultBase<object, DeleteManyDeliveriesDto>
    {
        public DeleteDeliveries(DeleteManyDeliveriesDto dto)
            : base(dto, ApiResources.Warehouses, "deliveries/delete_many")
        {
        }
    }
}