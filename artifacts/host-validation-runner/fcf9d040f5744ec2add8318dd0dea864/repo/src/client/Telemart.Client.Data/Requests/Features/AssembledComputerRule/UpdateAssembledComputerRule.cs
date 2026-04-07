using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssembledComputerRule
{
    public sealed class UpdateAssembledComputerRule : UpdateEntityResultRequestBase<AssembledComputerRuleDto, AssembledComputerRuleUpdateDto>
    {
        public UpdateAssembledComputerRule(int groupId, AssembledComputerRuleUpdateDto saveDto)
            : base(saveDto, ApiResources.AssembledComputerRules, groupId)
        {
        }
    }
}