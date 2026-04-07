namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public sealed class PackListChoiceParameter
    {
        public PackListChoiceParameter(int warehouseId)
        {
            WarehouseId = warehouseId;
        }

        public int WarehouseId { get; }
    }
}