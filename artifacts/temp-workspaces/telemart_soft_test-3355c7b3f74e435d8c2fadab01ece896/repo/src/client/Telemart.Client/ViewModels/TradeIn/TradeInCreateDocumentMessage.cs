using Telemart.Client.Common.Messages;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.ViewModels.TradeIn
{
    public class TradeInCreateDocumentMessage : EntityMessage<TradeInDocumentDto>
    {
        public TradeInCreateDocumentMessage(TradeInDocumentDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}