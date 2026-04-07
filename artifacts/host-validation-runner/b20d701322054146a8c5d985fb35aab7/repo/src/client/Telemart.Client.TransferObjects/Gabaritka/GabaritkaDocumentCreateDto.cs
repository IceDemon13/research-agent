using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Gabaritka
{
    public sealed class GabaritkaDocumentCreateDto
    {
        public GabaritkaDocumentCreateDto(int orderId, IReadOnlyCollection<GabaritkaDocumentPlaceCreateDto> places)
        {
            OrderId = orderId;
            Places = places;
        }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("places")]
        public IReadOnlyCollection<GabaritkaDocumentPlaceCreateDto> Places { get; set; }
    }
}