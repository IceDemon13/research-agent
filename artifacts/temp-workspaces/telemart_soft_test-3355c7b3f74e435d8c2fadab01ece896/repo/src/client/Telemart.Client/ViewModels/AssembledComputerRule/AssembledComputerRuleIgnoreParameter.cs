using System.Collections.Generic;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public sealed class AssembledComputerRuleIgnoreParameter
    {
        public AssembledComputerRuleIgnoreParameter(IReadOnlyCollection<int> ignoreSlotConsumerIds)
        {
            IgnoreSlotConsumerIds = ignoreSlotConsumerIds;
        }

        public IReadOnlyCollection<int> IgnoreSlotConsumerIds { get; }
    }
}