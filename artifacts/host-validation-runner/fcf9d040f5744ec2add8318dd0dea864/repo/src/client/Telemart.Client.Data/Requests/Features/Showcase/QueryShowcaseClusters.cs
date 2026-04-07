using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public sealed class QueryShowcaseClusters : QueryEntitiesRequestBase<ShowcaseClusterDto>
    {
        public QueryShowcaseClusters(params int[] categoryIds)
            : base(new ShowcaseClusterFilter(categoryIds), ApiResources.Showcases, "clusters")
        {
        }

        public class ShowcaseClusterFilter : FilteringItemBase
        {
            public ShowcaseClusterFilter(params int[] categoryIds)
            {
                CategoryIds = categoryIds;
            }

            [FilteringItemProperty("category_ids")]
            public int[] CategoryIds { get; }
        }
    }
}