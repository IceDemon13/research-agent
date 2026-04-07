using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AssemblyFullRule;

namespace Telemart.Client.Data.Requests.Features.AssemblyFullRule
{
    public class CreateAssemblyFullRule : CreateEntityResultRequestBase<AssemblyFullRuleDto, AssemblyFullRuleSaveDto>
    {
        public CreateAssemblyFullRule(AssemblyFullRuleSaveDto dto)
            : base(dto, ApiResources.AssemblyFull)
        {
        }
    }
}