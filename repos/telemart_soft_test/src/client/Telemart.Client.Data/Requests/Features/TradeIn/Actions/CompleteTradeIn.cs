using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public sealed class CompleteTradeIn : CallEntityActionRequestResultBase<TradeInDto>
    {
        public CompleteTradeIn(int id)
            : base(id, ApiResources.TradeIns, "complete")
        {
        }
    }
}