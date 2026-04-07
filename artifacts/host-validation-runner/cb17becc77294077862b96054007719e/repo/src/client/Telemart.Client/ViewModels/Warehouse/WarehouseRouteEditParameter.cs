namespace Telemart.Client.ViewModels.Warehouse
{
    public class WarehouseRouteEditParameter
    {
        public WarehouseRouteEditParameter(int warehouseId, int routeId, int? routeTimeId = null)
        {
            RouteId = routeId;
            RouteTimeId = routeTimeId;
            WarehouseId = warehouseId;
        }

        public int RouteId { get; set; }

        public int? RouteTimeId { get; set; }

        public int WarehouseId { get; set; }
    }
}
