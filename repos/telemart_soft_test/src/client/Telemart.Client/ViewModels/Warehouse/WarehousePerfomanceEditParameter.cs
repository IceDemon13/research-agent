using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse
{
    public class WarehousePerfomanceEditParameter : EditorParameter
    {
        public WarehousePerfomanceEditParameter(int warehouseId, int deliveryId)
           : base(deliveryId)
        {
            WarehouseId = warehouseId;
        }

        public int WarehouseId { get; }
    }
}
