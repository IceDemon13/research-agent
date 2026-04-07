namespace Telemart.Client.ViewModels.Warehouse
{
    public class WarehouseRouteCreateParameter
    {
        public WarehouseRouteCreateParameter(int warehouseFromId)
        {
            WarehouseFromId = warehouseFromId;
        }

        public int WarehouseFromId { get; set; }
    }
}
