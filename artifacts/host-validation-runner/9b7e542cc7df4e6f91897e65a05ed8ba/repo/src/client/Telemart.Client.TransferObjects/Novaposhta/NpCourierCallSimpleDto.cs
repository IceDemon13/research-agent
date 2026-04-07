using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Novaposhta
{
    public class NpCourierCallSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("barcode")]
        public string Barcode { get; init; }

        [JsonProperty("date")]
        public DateTime Date { get; init; }

        [JsonProperty("time_from")]
        public TimeOnly TimeFrom { get; init; }

        [JsonProperty("time_to")]
        public TimeOnly TimeTo { get; init; }

        [JsonProperty("status")]
        public string Status { get; init; }

        [JsonProperty("completed")]
        public bool Completed { get; init; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }
    }
}
