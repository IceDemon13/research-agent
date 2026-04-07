using Telemart.Client.TransferObjects.HotlineCompetitor;

namespace Telemart.Client.Common.Messages
{
    public class HotlineCompetitorMessage : EntityMessage<HotlineCompetitorDto>
    {
        public HotlineCompetitorMessage(HotlineCompetitorDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
