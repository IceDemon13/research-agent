using Telemart.Client.Common.Messages;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInMessage : EntityMessage<TradeInDto>
    {
        public TradeInMessage(TradeInDto tradeInDto, MessageType messageType)
            : base(tradeInDto, messageType)
        {
        }
    }
}