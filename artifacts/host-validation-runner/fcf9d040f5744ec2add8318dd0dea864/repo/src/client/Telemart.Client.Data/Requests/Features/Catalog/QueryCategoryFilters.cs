using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.Filter;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryCategoryFilters : CallActionWithBodyRequestBase<QueryFiltersResponse, QueryCategoryFilters.QueryFiltersRequest>
    {
        public QueryCategoryFilters(int categoryId, int contractorId)
            : base(new QueryFiltersRequest(categoryId, contractorId), "products", "query_filters")
        {
        }

        public class QueryFiltersRequest
        {
            public QueryFiltersRequest(int categoryId, int contractorId)
            {
                CategoryId = categoryId;
                ContractorId = contractorId;
                Active = null;
                ShowArchive = true;
            }

            [JsonProperty("category_id")]
            public int CategoryId { get; set; }

            [JsonProperty("contractor_id")]
            public int ContractorId { get; set; }

            [JsonProperty("active")]
            public double? Active { get; set; }

            [JsonProperty("show_archive")]
            public bool? ShowArchive { get; set; }
        }
    }
}