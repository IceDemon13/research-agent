using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Task
{
    public class PickupProductsDto
    {
        public PickupProductsDto(List<PickupProductDto> products)
        {
            Products = products;
        }

        public PickupProductsDto()
        {
        }

        [JsonProperty("products")]
        public List<PickupProductDto> Products { get; set; }
    }
}