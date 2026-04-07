using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Common.Messages
{
    public class CityMessage : EntityMessage<CityDto>
    {
        public CityMessage(CityDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}
