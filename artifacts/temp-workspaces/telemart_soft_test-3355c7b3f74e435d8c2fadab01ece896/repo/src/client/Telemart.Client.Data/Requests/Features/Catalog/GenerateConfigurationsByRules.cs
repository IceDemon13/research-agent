using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.Data.Requests.Features.Catalog.TransferObjects;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Data.Requests.Features.Catalog
{
    public class GenerateConfigurationsByRules : CallActionWithBodyRequestBase<ComplectationsGeneratorDto, GenerateConfigurationsByRules.GenerateAssemblyComplectationsByRulesRequest>
    {
        public GenerateConfigurationsByRules(decimal? maxPrice, bool debug, StockStrategy? stockStrategy, bool? useTransit, bool? usePurchases, params int[] ruleIds)
            : base(new GenerateAssemblyComplectationsByRulesRequest(ruleIds, maxPrice, debug, stockStrategy, useTransit, usePurchases), "assembly_complectations/rules", "generate")
        {
        }

        public class GenerateAssemblyComplectationsByRulesRequest
        {
            public GenerateAssemblyComplectationsByRulesRequest(
                int[] ruleIds,
                decimal? maxPrice,
                bool debug,
                StockStrategy? stockStrategy,
                bool? useTransit,
                bool? usePurchases)
            {
                RuleIds = ruleIds;
                MaxPrice = maxPrice;
                Debug = debug;
                StockStrategy = stockStrategy;
                UseTransits = useTransit;
                UsePurchases = usePurchases;
            }

            [JsonProperty("rule_ids")]
            public int[] RuleIds { get; init; }

            [JsonProperty("max_price")]
            public decimal? MaxPrice { get; init; }

            [JsonProperty("debug")]
            public bool Debug { get; init; }

            [JsonProperty("stock_strategy")]
            public StockStrategy? StockStrategy { get; init; }

            [JsonProperty("use_transits")]
            public bool? UseTransits { get; init; }

            [JsonProperty("use_purchases")]
            public bool? UsePurchases { get; init; }
        }
    }
}