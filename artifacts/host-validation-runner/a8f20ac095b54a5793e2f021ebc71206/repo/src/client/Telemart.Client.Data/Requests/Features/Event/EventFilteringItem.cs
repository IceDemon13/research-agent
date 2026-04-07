using Telemart.Client.Data.WebClient;

namespace Telemart.Client.Data.Requests.Features.Event
{
    public class EventFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("entity_id")]
        public int? EntityId { get; init; }

        [FilteringItemProperty("type_id")]
        public int? TypeId { get; init; }

        [FilteringItemProperty("completed")]
        public bool? Completed { get; init; }
    }
}