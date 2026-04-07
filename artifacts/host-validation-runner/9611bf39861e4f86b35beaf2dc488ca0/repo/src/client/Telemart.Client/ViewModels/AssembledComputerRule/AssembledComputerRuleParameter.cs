using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class AssembledComputerRuleParameter : EditorParameter
    {
        public AssembledComputerRuleParameter(int assembledComputerRuleId, string productName = null)
            : base(assembledComputerRuleId)
        {
            ProductName = productName;
        }

        public string ProductName { get; init; }
    }
}
