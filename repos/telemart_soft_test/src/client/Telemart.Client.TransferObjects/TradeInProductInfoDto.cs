namespace Telemart.Client.TransferObjects
{
    public class TradeInProductInfoDto
    {
        public TradeInProductInfoDto(int? orderId, int? orderStateId)
        {
            OrderId = orderId;
            OrderStateId = orderStateId;
        }

        public int? OrderId { get; set; }

        public int? OrderStateId { get; set; }
    }
}