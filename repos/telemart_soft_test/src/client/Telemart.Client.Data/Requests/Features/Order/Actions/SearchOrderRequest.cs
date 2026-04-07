using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public sealed class SearchOrderRequest : CallActionWithBodyRequestResultBase<OrderReceiveMoneyDto, SearchOrderRequest.OrderSerarchDto>
    {
        public SearchOrderRequest(int orderId)
            : base(new OrderSerarchDto(orderId), ApiResources.Orders, "search")
        {
        }

        public SearchOrderRequest(string trackNumber)
            : base(new OrderSerarchDto(trackNumber), ApiResources.Orders, "search")
        {
        }

        public class OrderSerarchDto
        {
            public OrderSerarchDto(int orderId)
            {
                SearchType = 2;
                OrderId = orderId;
                TrackNumber = null;
            }

            public OrderSerarchDto(string trackNumber)
            {
                SearchType = 1;
                OrderId = null;
                TrackNumber = trackNumber;
            }

            [JsonProperty("search_type")]
            public int SearchType { get; set; }

            [JsonProperty("order_id")]
            public int? OrderId { get; set; }

            [JsonProperty("track_number")]
            public string TrackNumber { get; set; }
        }
    }
}