using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;
using static Telemart.Client.Data.Requests.Features.AssembledComputerRule.QueryAssembledComputerRuleReserve;

namespace Telemart.Client.Data.Requests.Features.AssembledComputerRule
{
    public sealed class QueryAssembledComputerRuleReserve : CallActionWithBodyRequestResultBase<List<AssembledComputerRuleReserveProductDto>, AssembledComputerRuleReserveFilteringItem>
    {
        public QueryAssembledComputerRuleReserve(int[] productIds = null, int? minReserveQuantity = null)
            : base(new AssembledComputerRuleReserveFilteringItem(productIds, minReserveQuantity), ApiResources.AssembledComputerRules, "get_reserve")
        {
        }

        public sealed class AssembledComputerRuleReserveFilteringItem : FilteringItemBase
        {
            public AssembledComputerRuleReserveFilteringItem(int[] productIds, int? minReserveQuantity)
            {
                ProductIds = productIds;
                MinReserveQuantity = minReserveQuantity;
            }

            [JsonProperty("min_reserve_quantity")]
            public int? MinReserveQuantity { get; set; }

            [JsonProperty("product_ids")]
            public int[] ProductIds { get; set; }
        }
    }
}