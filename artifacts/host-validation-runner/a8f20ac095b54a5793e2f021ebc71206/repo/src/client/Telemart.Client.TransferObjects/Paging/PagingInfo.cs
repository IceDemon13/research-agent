using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Paging
{
    public class PagingInfo
    {
        public const string SkipName = "skip";
        public const string TakeName = "take";

        [JsonProperty("returned")]
        public int Returned { get; set; }

        [JsonProperty(SkipName)]
        public int Skip { get; set; }

        [JsonProperty(TakeName)]
        public int Take { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        public bool HasMoreRows => Skip + Returned < TotalCount;
    }
}