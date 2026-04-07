using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public class PackListParameter : EditorParameter
    {
        public PackListParameter(int id, int warehouseId)
            : base(id)
        {
            WarehouseId = warehouseId;
        }

        public int WarehouseId { get; }
    }
}
