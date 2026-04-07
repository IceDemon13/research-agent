using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Common.Messages
{
    public class ForeignCityMessage : EntityMessage<ForeignCityDto>
    {
        public ForeignCityMessage(ForeignCityDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}