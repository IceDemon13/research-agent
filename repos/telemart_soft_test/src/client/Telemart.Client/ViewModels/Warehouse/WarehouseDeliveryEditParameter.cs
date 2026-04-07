using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse
{
    public class WarehouseDeliveryEditParameter : EditorParameter
    {
        public WarehouseDeliveryEditParameter(int warehouseId, int deliveryId)
            : base(deliveryId)
        {
            WarehouseId = warehouseId;
        }

        public int WarehouseId { get; }
    }
}
