using Telemart.Client.TransferObjects.PosTerminal;

namespace Telemart.Client.Common.Messages
{
    public class PosSettingsMessage : EntityMessage<PosSettingsDto>
    {
        public PosSettingsMessage(PosSettingsDto entity, MessageType messageType)
                : base(entity, messageType)
        {
        }
    }
}