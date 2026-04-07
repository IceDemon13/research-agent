using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Paging
{
    public class PagedResult<T>
        where T : new()
    {
        [JsonProperty("data")]
        public List<T> Data { get; set; }

        [JsonProperty("pagination")]
        public PagingInfo Pagination { get; set; }
    }
}