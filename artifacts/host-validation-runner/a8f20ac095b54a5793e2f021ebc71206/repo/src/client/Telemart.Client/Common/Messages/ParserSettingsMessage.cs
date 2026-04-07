using Telemart.Client.TransferObjects.ParserSettings;

namespace Telemart.Client.Common.Messages
{
    public class ParserSettingsMessage : EntityMessage<ParserSettingsDto>
    {
        public ParserSettingsMessage(ParserSettingsDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
