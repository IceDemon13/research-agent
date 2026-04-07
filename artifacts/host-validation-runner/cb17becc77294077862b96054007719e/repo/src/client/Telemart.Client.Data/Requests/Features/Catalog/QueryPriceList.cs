using System.Net.Http;
using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public sealed class QueryPriceList : RestClientGatewayRequestBase<QueryPriceList.DownloadPriceRequest>
    {
        public QueryPriceList(int contractorId, int[] categoryIds)
            : base(HttpMethod.Post)
        {
            PathParameters = new[] { "download/pricelist" };

            Body = new DownloadPriceRequest { ContractorId = contractorId, CategoryIds = categoryIds };
        }

        public class DownloadPriceRequest
        {
            [JsonProperty("contractor_id")]
            public int ContractorId { get; set; }

            [JsonProperty("category_ids")]
            public int[] CategoryIds { get; set; }
        }
    }
}