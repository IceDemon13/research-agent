namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public class SelectAssembledComputerRuleParameter
    {
        public SelectAssembledComputerRuleParameter(int contractorId)
        {
            ContractorId = contractorId;
        }

        public int ContractorId { get; }
    }
}