using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public class NewPostGetDeliveryDateRequest
    {
        public NewPostGetDeliveryDateRequest(int citySenderId, int cityRecipientId, int carryTypeId, DateTime dateTime)
        {
            CitySenderId = citySenderId;
            CityRecipientId = cityRecipientId;
            CarryTypeId = carryTypeId;
            DateTime = dateTime;
        }

        [JsonProperty("city_sender_id")]
        public int CitySenderId { get; set; }

        [JsonProperty("city_recipient_id")]
        public int CityRecipientId { get; set; }

        [JsonProperty("carry_type_id")]
        public int CarryTypeId { get; set; }

        [JsonProperty("date_time")]
        public DateTime DateTime { get; set; }
    }
}