using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public class UpdateTradeIn : UpdateEntityResultRequestBase<TradeInDto, TradeInUpdateDto>
    {
        public UpdateTradeIn(int id, TradeInUpdateDto editDto)
            : base(editDto, ApiResources.TradeIns, id)
        {
        }
    }
}