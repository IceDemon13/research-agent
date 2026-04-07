using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductDescriptionSaveResponse
    {
        [JsonProperty("results")]
        public List<ProductDescriptionSaveResult> Results { get; set; }
    }
}