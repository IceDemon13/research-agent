namespace Telemart.Client.ViewModels.Warehouse
{
    public sealed class WarehouseCopyParameter
    {
        public WarehouseCopyParameter(int warehouseId)
        {
            WarehouseId = warehouseId;
        }

        public int WarehouseId { get; }
    }
}