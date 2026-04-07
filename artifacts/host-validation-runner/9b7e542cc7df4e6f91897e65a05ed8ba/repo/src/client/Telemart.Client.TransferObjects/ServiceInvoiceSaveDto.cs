using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ServiceInvoiceSaveDto
    {
        [JsonProperty("products")]
        public List<ServiceInvoiceProductDto> Products { get; set; }
    }
}