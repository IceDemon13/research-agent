using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSearchTemplate
{
    public sealed class ProductSearchTemplateDto
    {
        [JsonProperty("parser_search_template_id")]
        public int ParserSearchTemplateId { get; set; }

        [JsonProperty("parser_alias_id")]
        public long ParserAliasId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("contractor_product_name")]
        public string ContractorProductName { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("features")]
        public IReadOnlyCollection<ProductFeatureValueSearchTemplateDto> Features { get; set; }
    }
}