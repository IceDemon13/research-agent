using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public sealed class OverReceiveTradeIn : CallEntityActionRequestResultBase<TradeInDto>
    {
        public OverReceiveTradeIn(int id)
            : base(id, ApiResources.TradeIns, "over_receive")
        {
        }
    }
}