namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public sealed class PackListTransferPackParameter
    {
        public PackListTransferPackParameter(int packListId, int warehouseId)
        {
            PackListId = packListId;
            WarehouseId = warehouseId;
        }

        public int PackListId { get; }

        public int WarehouseId { get; }
    }
}