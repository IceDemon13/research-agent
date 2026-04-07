using Telemart.Client.TransferObjects.FiscalRegistrar;

namespace Telemart.Client.Common.Messages
{
    public sealed class FiscalRegistrarSettingsMessage : EntityMessage<FiscalRegistrarSettingsDto>
    {
        public FiscalRegistrarSettingsMessage(FiscalRegistrarSettingsDto dto, MessageType messageType)
            : base(dto, messageType)
        {
        }
    }
}