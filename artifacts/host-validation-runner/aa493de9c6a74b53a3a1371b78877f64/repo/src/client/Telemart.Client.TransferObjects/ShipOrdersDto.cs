using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ShipOrdersDto
    {
        [JsonProperty("order_ids")]
        public List<int> OrderIds { get; set; }
    }
}