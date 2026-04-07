using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn;

public sealed class QueryTradeInMaxPrice : CallActionWithBodyRequestResultBase<TradeInMaxPriceDto, TradeInMaxPriceGetDto>
{
    public QueryTradeInMaxPrice(TradeInMaxPriceGetDto dto)
        : base(dto, ApiResources.TradeIns, "max_price")
    {
    }
}