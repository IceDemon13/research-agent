using System.Collections.Generic;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public sealed class AssembledComputerRuleCreateCategoryParameter
    {
        public AssembledComputerRuleCreateCategoryParameter(Dictionary<int, int[]> categoryProductTypeIds)
        {
            CategoryProductTypeIds = categoryProductTypeIds;
        }

        public Dictionary<int, int[]> CategoryProductTypeIds { get; init; }
    }
}
