using Telemart.Client.Common.Messages;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.ViewModels.Locations
{
    public sealed class LocationMessage : EntityMessage<LocationEntityDto>
    {
        public LocationMessage(LocationEntityDto location, MessageType messageType)
            : base(location, messageType)
        {
        }
    }
}