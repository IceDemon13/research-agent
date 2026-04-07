namespace Telemart.Client.ViewModels.Store.Order
{
    public class OrderSetManagerParameter
    {
        public OrderSetManagerParameter(int orderId, int managerId)
        {
            OrderId = orderId;
            ManagerId = managerId;
        }

        public int OrderId { get; }

        public int ManagerId { get; }
    }
}
