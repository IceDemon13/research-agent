using System.Collections.Generic;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class GeneratedAssembledComputerRuleParameter
    {
        public GeneratedAssembledComputerRuleParameter(IReadOnlyCollection<GeneratedAssembledComputerRuleProductViewItem> products)
        {
            Products = products;
        }

        public IReadOnlyCollection<GeneratedAssembledComputerRuleProductViewItem> Products { get; }
    }
}