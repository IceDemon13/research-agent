using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderGiveInfoDto
    {
        [JsonProperty("order")]
        public OrderDto Order { get; set; }

        [JsonProperty("left_to_pay")]
        public PriceDto LeftToPay { get; set; }

        [JsonProperty("order_cells")]
        public List<OrderCellDto> OrderCells { get; set; }
    }
}