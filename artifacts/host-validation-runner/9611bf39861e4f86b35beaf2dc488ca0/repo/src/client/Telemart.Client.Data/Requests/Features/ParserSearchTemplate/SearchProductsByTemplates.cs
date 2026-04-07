using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ParserSearchTemplate;

namespace Telemart.Client.Data.Requests.Features.ParserSearchTemplate
{
    public sealed class SearchProductsByTemplates : CallActionWithBodyRequestResultBase<IReadOnlyCollection<ProductSearchTemplateDto>, ProductSearchRequest>
    {
        public SearchProductsByTemplates(IReadOnlyCollection<int> templateIds)
            : base(new ProductSearchRequest(templateIds), ApiResources.ParserSearchTemplates, "search_products")
        {
        }
    }

    public class ProductSearchRequest
    {
        public ProductSearchRequest(IReadOnlyCollection<int> templateIds)
        {
            TemplateIds = templateIds;
        }

        [JsonProperty("parser_search_template_ids")]
        public IReadOnlyCollection<int> TemplateIds { get; set; }
    }
}