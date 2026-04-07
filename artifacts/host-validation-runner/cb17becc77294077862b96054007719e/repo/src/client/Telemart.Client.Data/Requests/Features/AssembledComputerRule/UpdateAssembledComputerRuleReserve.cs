using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssembledComputerRule
{
    public class UpdateAssembledComputerRuleReserve : CallActionWithBodyRequestBase<object, AssembledComputerRuleReserveUpdateDto>
    {
        public UpdateAssembledComputerRuleReserve(AssembledComputerRuleReserveUpdateDto dto)
            : base(dto, ApiResources.AssembledComputerRules, "update_reserve")
        {
        }
    }
}