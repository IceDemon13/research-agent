using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects
{
    public sealed class ProductsPagedResult : PagedResult<ProductDto>
    {
        [JsonProperty("categories")]
        public IReadOnlyCollection<CatalogCategoryDto> Categories { get; set; }
    }
}