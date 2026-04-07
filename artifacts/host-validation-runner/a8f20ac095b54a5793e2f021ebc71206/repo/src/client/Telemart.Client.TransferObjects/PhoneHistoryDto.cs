using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class PhoneHistoryDto
    {
        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("document_number")]
        public int DocumentNumber { get; set; }

        [JsonProperty("phone1")]
        public string Phone1 { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("is_last_phone")]
        public bool IsLastPhone { get; set; }

        [JsonProperty("client_id")]
        public int? CustomerId { get; set; }
    }
}