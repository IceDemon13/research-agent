using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AreaDto
    {
        public AreaDto(
            int id,
            string name,
            string nameUkr,
            string nameEn,
            bool active,
            string npAreaRef,
            string meAreaRef,
            List<int> upAreaIds)
        {
            Id = id;
            Name = name;
            NameUkr = nameUkr;
            NameEn = nameEn;
            Active = active;
            NpAreaRef = npAreaRef;
            MeAreaRef = meAreaRef;
            UpAreaIds = upAreaIds;
        }

        public AreaDto()
        {
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("np_area_ref")]
        public string NpAreaRef { get; set; }

        [JsonProperty("me_area_ref")]
        public string MeAreaRef { get; set; }

        [JsonProperty("up_area_ids")]
        public List<int> UpAreaIds { get; set; }
    }
}