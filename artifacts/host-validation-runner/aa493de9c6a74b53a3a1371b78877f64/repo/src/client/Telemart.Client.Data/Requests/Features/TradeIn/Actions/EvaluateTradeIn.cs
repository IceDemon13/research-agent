using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public sealed class EvaluateTradeIn : CallEntityActionWithBodyRequestResultBase<TradeInDto, EvaluateTradeInDto>
    {
        public EvaluateTradeIn(EvaluateTradeInDto dto)
            : base(dto.Id, dto, ApiResources.TradeIns, "evaluate")
        {
        }
    }
}