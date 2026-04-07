using Telemart.Client.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class AdditionalServiceProductEntityMessage : EntityMessage<AdditionalServiceProductDto>
    {
        public AdditionalServiceProductEntityMessage(AdditionalServiceProductDto dto, MessageType messageType)
            : base(dto, messageType)
        {
        }
    }
}