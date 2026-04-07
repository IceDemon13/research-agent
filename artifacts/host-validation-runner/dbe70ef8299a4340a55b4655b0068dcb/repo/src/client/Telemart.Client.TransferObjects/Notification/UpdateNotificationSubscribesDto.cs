using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Notification
{
    public sealed record UpdateNotificationSubscribesDto
    {
        public UpdateNotificationSubscribesDto(IReadOnlyCollection<UpdateNotificationSubscribeDto> subscribes)
        {
            Subscribes = subscribes;
        }

        [JsonProperty("subscribes")]
        public IReadOnlyCollection<UpdateNotificationSubscribeDto> Subscribes { get; init; }
    }
}