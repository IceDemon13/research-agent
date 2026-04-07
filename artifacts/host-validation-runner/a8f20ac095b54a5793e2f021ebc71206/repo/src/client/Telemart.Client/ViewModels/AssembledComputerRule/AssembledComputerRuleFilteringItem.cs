using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRuleFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("product_name")]
        public string ProductName { get; set; }
    }
}
