using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse
{
    public class WarehouseCellParameter : EditorParameter
    {
        public WarehouseCellParameter(int id, int warehouseId)
            : base(id)
        {
            WarehouseId = warehouseId;
        }

        public int WarehouseId { get; }
    }
}
