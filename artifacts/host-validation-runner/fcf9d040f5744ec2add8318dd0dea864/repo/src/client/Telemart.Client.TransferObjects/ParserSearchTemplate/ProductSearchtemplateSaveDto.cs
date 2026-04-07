using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSearchTemplate
{
    public sealed class ProductSearchtemplateSaveDto
    {
        public ProductSearchtemplateSaveDto(
            ProductSearchTemplateSaveItemDto[] results,
            IReadOnlyCollection<int> parserSearchTemplateIds)
        {
            Results = results;
            ParserSearchTemplateIds = parserSearchTemplateIds;
        }

        [JsonProperty("results")]
        public ProductSearchTemplateSaveItemDto[] Results { get; set; }

        [JsonProperty("parser_search_template_ids")]
        public IReadOnlyCollection<int> ParserSearchTemplateIds { get; set; }
    }
}