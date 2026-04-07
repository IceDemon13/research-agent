using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Debezium;

namespace Telemart.Client.Data.Requests.Features.Showcase.Actions
{
    public sealed class QueryShowcaseHistories : CallActionWithBodyRequestResultBase<ShowcaseHistoriesDto, QueryShowcaseHistories.ShowcaseHistoriesRequest>
    {
        public QueryShowcaseHistories(int productId)
            : base(new ShowcaseHistoriesRequest(productId), ApiResources.Showcases, "showcase_history")
        {
        }

        public sealed class ShowcaseHistoriesRequest
        {
            public ShowcaseHistoriesRequest(int productId)
            {
                ProductId = productId;
            }

            [JsonProperty("product_id")]
            public int ProductId { get; }
        }
    }
}