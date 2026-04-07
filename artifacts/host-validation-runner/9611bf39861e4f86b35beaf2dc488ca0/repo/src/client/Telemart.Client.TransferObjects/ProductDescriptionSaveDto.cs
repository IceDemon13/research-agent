using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductDescriptionSaveDto
    {
        public ProductDescriptionSaveDto(int productId, int languageId, string description)
        {
            ProductId = productId;
            LanguageId = languageId;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("language_id")]
        public int LanguageId { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}