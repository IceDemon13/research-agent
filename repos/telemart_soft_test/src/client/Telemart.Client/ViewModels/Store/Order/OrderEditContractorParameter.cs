namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderEditContractorParameter
    {
        public OrderEditContractorParameter(int orderId, int clientId)
        {
            OrderId = orderId;
            ClientId = clientId;
        }

        public int OrderId { get; }

        public int ClientId { get; }
    }
}
