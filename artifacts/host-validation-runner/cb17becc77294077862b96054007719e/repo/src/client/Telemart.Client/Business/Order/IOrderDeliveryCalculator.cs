using System.Collections.Generic;
using System.Threading.Tasks;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Business.Order
{
    public interface IOrderDeliveryCalculator
    {
        Task<OrderDeliveryTime> CalculateOrderDeliveryTimeAsync(
            int orderId,
            int? warehouseId,
            int? assemblyWarehouseId,
            int? bufferWarehouseId,
            int? additionalServiceWarehouseId,
            int carryTypeId,
            int subdivisionId,
            OrderStatus orderState,
            IReadOnlyCollection<IOrderProduct> orderProducts,
            IReadOnlyCollection<OrderFolderDto> folders);
    }
}