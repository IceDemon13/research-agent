using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class UnlockTradeIn : UnlockRequestBase<TradeInDto>
    {
        public UnlockTradeIn(int id, bool force = false)
            : base(force, ApiResources.TradeIns, id)
        {
        }
    }
}