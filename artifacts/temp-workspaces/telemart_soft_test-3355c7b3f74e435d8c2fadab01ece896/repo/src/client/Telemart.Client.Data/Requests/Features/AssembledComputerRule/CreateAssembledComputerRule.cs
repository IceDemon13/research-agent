using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssembledComputerRule
{
    public sealed class CreateAssembledComputerRule : CreateEntityResultRequestBase<AssembledComputerRuleDto, AssembledComputerRuleCreateDto>
    {
        public CreateAssembledComputerRule(AssembledComputerRuleCreateDto dto)
            : base(dto, ApiResources.AssembledComputerRules)
        {
        }
    }
}