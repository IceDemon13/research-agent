namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderPackCellParameter
    {
        public OrderPackCellParameter(int[] cellIds, int warehouseId)
        {
            CellIds = cellIds;
            WarehouseId = warehouseId;
        }

        public int[] CellIds { get; }

        public int WarehouseId { get; }
    }
}
