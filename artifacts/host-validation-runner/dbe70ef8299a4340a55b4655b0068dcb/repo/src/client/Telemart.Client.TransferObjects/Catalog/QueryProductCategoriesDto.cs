using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Catalog
{
    public sealed class QueryProductCategoriesDto
    {
        public QueryProductCategoriesDto(IReadOnlyCollection<int> productIds)
        {
            ProductIds = productIds;
        }

        [JsonProperty("product_ids")]
        public IReadOnlyCollection<int> ProductIds { get; }
    }
}