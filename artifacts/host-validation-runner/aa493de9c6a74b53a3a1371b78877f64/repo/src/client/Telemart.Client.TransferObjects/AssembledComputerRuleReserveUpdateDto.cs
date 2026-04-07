using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AssembledComputerRuleReserveUpdateDto
    {
        public AssembledComputerRuleReserveUpdateDto(List<AssembledComputerRuleProductReserveUpdateDto> products)
        {
            Products = products;
        }

        [JsonProperty("products")]
        public List<AssembledComputerRuleProductReserveUpdateDto> Products { get; set; }
    }
}