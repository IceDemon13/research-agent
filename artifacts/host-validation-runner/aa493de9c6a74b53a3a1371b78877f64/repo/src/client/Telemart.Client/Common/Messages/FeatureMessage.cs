using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Common.Messages
{
    public sealed class FeatureMessage : EntityMessage<FeatureFullDto>
    {
        public FeatureMessage(FeatureFullDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
