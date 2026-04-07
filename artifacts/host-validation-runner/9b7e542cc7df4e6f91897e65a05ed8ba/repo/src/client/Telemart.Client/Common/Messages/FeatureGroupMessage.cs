using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Common.Messages
{
    public class FeatureGroupMessage : EntityMessage<FeatureGroupSimpleDto>
    {
        public FeatureGroupMessage(FeatureGroupSimpleDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
