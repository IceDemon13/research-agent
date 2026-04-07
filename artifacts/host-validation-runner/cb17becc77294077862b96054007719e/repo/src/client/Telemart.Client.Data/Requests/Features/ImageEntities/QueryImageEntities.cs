using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.ImageEntities;

namespace Telemart.Client.Data.Requests.Features.ImageEntities
{
    public class QueryImageEntities : QueryEntitiesRequestBase<ImageEntityDto>
    {
        public QueryImageEntities(IFilteringItem filter)
            : base(filter, ApiResources.Images)
        {
        }
    }

    public sealed class ImageEntitiesFilter : FilteringItemBase
    {
        public ImageEntitiesFilter(string entityType, params int[] entityIds)
        {
            EntityType = entityType;
            EntityIds = entityIds;
        }

        [FilteringItemProperty("entity_type")]
        public string EntityType { get; }

        [FilteringItemProperty("entity_ids")]
        public int[] EntityIds { get; }
    }
}