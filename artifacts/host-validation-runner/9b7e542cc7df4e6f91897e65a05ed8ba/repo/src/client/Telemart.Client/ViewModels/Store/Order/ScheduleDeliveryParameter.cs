namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class ScheduleDeliveryParameter
    {
        public ScheduleDeliveryParameter(int orderId)
        {
            OrderId = orderId;
        }

        public int OrderId { get; }
    }
}