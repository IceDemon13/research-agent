using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using static Telemart.Client.Data.Requests.Features.Order.CreateJoker;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class CreateJoker : CreateEntityResultRequestBase<OrderDto, JokerCreateDto>
    {
        public CreateJoker(int orderId, List<int> minusHashtagIds)
            : base(new JokerCreateDto(orderId, minusHashtagIds), $"{ApiResources.Orders}/create_joker")
        {
        }

        public class JokerCreateDto
        {
            public JokerCreateDto(int orderId, List<int> minusHashtagIds)
            {
                OrderId = orderId;
                MinusHashtagIds = minusHashtagIds;
            }

            [JsonProperty("order_id")]
            public int OrderId { get; set; }

            [JsonProperty("minus_hashtag_ids")]
            public List<int> MinusHashtagIds { get; set; }
        }
    }
}
