using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public sealed class CancelTradeIn : CallEntityActionWithBodyRequestResultBase<TradeInDto, TradeInCancelDto>
    {
        public CancelTradeIn(TradeInCancelDto dto)
            : base(dto.Id, dto, ApiResources.TradeIns, "cancel")
        {
        }
    }
}