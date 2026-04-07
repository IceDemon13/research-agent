using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ConfirmReturnInvoiceDto
    {
        public ConfirmReturnInvoiceDto(List<ConfirmReturnInvoiceProductDto> products)
        {
            Products = products;
        }

        [JsonProperty("products")]
        public List<ConfirmReturnInvoiceProductDto> Products { get; set; }
    }
}