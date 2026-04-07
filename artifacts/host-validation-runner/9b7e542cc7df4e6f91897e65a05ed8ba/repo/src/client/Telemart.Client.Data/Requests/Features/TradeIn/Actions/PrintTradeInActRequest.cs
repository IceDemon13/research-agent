using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public sealed class PrintTradeInActRequest : CallEntityActionRequestResultBase<TradeInDto>
    {
        public PrintTradeInActRequest(int id)
            : base(id, ApiResources.TradeIns, "print_act")
        {
        }
    }
}