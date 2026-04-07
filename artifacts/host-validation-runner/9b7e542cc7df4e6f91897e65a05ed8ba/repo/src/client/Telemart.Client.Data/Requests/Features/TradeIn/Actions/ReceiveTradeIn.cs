using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public sealed class ReceiveTradeIn : CallEntityActionRequestResultBase<TradeInDto>
    {
        public ReceiveTradeIn(int id)
            : base(id, ApiResources.TradeIns, "receive")
        {
        }
    }
}