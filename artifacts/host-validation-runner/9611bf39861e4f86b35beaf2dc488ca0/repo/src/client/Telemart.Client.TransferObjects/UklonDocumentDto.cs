using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record UklonDocumentDto
    {
        [JsonProperty("id")]
        public string Id { get; init; }

        [JsonProperty("fare")]
        public UklonFareDto Fare { get; init; }

        [JsonProperty("driver")]
        public UklonDriverDto Driver { get; init; }

        [JsonProperty("car")]
        public UklonCarDto Car { get; init; }

        [JsonProperty("receiver_address")]
        public UklonAddressDto ReceiverAddress { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("send_date")]
        public DateTime? SendDate { get; init; }

        [JsonProperty("arrival_date")]
        public DateTime? ArrivalDate { get; init; }

        [JsonProperty("receive_date")]
        public DateTime? ReceiveDate { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }
    }
}