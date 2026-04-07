namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class CreateUnpackOrderEventParameter
    {
        public CreateUnpackOrderEventParameter(int orderId, int warehouseId, bool eventForWarehouseEmployees, bool taskForPickupEmployees)
        {
            OrderId = orderId;
            WarehouseId = warehouseId;
            EventForWarehouseEmployees = eventForWarehouseEmployees;
            TaskForPickupEmployees = taskForPickupEmployees;
        }

        public int OrderId { get; }

        public int WarehouseId { get; }

        public bool EventForWarehouseEmployees { get; }

        public bool TaskForPickupEmployees { get; }
    }
}