using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Promo
{
    [DataContract]
    public class PromoSimpleDto
    {
        [DataMember(Order = 1)]
        [JsonProperty("id")]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        [JsonProperty("title")]
        public string Title { get; set; }

        [DataMember(Order = 3)]
        [JsonProperty("title_ukr")]
        public string TitleUkr { get; set; }

        [DataMember(Order = 4)]
        [JsonProperty("date_start")]
        public DateTime DateStart { get; set; }

        [DataMember(Order = 5)]
        [JsonProperty("date_end")]
        public DateTime DateEnd { get; set; }

        [JsonProperty("title_en")]
        [DataMember(Order = 6)]
        public string TitleEn { get; set; }
    }
}