using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class UpdateOrderWarrantyDto
    {
        public UpdateOrderWarrantyDto(IReadOnlyCollection<UpdateOrderWarrantyProductDto> products)
        {
            Products = products;
        }

        [JsonProperty("products")]
        public IReadOnlyCollection<UpdateOrderWarrantyProductDto> Products { get; set; }
    }
}