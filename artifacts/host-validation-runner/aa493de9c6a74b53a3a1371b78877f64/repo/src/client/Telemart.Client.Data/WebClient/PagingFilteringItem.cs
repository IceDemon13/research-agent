namespace Telemart.Client.Data.WebClient
{
    public class PagingFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("skip")]
        public int? Skip { get; set; }

        [FilteringItemProperty("take")]
        public int? Take { get; set; }
    }
}