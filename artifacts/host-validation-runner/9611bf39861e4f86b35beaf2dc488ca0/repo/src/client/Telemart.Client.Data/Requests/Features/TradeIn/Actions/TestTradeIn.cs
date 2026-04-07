using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public sealed class TestTradeIn : CallEntityActionRequestResultBase<TradeInDto>
    {
        public TestTradeIn(int id, bool tested)
            : base(id, ApiResources.TradeIns, $"test/{tested}")
        {
        }
    }
}