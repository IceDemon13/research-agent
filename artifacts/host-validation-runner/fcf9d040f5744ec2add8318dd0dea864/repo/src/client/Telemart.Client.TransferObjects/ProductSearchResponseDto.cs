using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductSearchResponseDto
    {
        [JsonProperty("pattern")]
        public string Pattern { get; set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<ProductSearchResponseProductDto> Products { get; set; }
    }
}