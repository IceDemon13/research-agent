using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderReconfirmDto
    {
        public OrderReconfirmDto(int id, int changeReasonId, string comment, int[] orderProductIds)
        {
            Id = id;
            OrderStateChangeReasonId = changeReasonId;
            Comment = comment;
            OrderProductIds = orderProductIds;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state_change_reason_id")]
        public int OrderStateChangeReasonId { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("order_product_ids")]
        public int[] OrderProductIds { get; set; }
    }
}