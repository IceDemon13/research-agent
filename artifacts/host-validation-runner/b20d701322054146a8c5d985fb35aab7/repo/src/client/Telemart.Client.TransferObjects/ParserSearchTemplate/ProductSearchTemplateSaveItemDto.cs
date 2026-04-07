using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSearchTemplate
{
    public sealed class ProductSearchTemplateSaveItemDto
    {
        public ProductSearchTemplateSaveItemDto(
            int parserSearchTemplateId,
            long parserAliasId,
            int contractorId,
            int productId,
            bool active)
        {
            ParserSearchTemplateId = parserSearchTemplateId;
            ParserAliasId = parserAliasId;
            ContractorId = contractorId;
            ProductId = productId;
            Active = active;
        }

        [JsonProperty("parser_search_template_id")]
        public int ParserSearchTemplateId { get; set; }

        [JsonProperty("parser_alias_id")]
        public long ParserAliasId { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}