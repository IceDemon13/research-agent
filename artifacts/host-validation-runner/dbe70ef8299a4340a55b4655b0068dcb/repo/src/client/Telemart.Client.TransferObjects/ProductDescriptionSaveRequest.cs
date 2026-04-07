using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductDescriptionSaveRequest
    {
        public ProductDescriptionSaveRequest(ProductDescriptionSaveDto[] descriptions)
        {
            Descriptions = descriptions ?? throw new ArgumentNullException(nameof(descriptions));
        }

        [JsonProperty("descriptions")]
        public ProductDescriptionSaveDto[] Descriptions { get; set; }
    }
}