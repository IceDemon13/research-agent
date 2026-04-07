using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Common.Messages
{
    public class ShowcaseMessage : EntityMessage<ShowcaseDto>
    {
        public ShowcaseMessage(ShowcaseDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
