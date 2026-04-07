using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Warehouse.Delivery;

namespace Telemart.Client.Data.Requests.Features.Warehouse.Delivery
{
    public sealed class CreateManyDeliveries : CallActionWithBodyRequestResultBase<List<DeliveryDto>, ManyDeliveriesSaveDto>
    {
        public CreateManyDeliveries(ManyDeliveriesSaveDto dto)
            : base(dto, ApiResources.Warehouses, $"{ApiResources.Deliveries}/create_many")
        {
        }
    }
}