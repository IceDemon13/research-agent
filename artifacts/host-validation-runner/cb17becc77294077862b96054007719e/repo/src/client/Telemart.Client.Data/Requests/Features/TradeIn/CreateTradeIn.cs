using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class CreateTradeIn : CreateEntityResultRequestBase<TradeInDto, TradeInCreateDto>
    {
        public CreateTradeIn(TradeInCreateDto createDto)
            : base(createDto, ApiResources.TradeIns)
        {
        }
    }
}