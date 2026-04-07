using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.RobotProperty;

namespace Telemart.Client.Data.Requests.Features.RobotProperty
{
    public sealed class QueryRobotCategoryPropertyValues : QueryEntitiesRequestBase<RobotCategoryPropertyValueDto>
    {
        public QueryRobotCategoryPropertyValues(int categoryId)
            : base(new RobotCategoryPropertiesFilter(categoryId), $"{ApiResources.RobotProperties}/values")
        {
        }

        private sealed class RobotCategoryPropertiesFilter : FilteringItemBase
        {
            public RobotCategoryPropertiesFilter(params int[] categoryIds)
            {
                CategoryIds = categoryIds;
            }

            [FilteringItemProperty("categoryIds")]
            public int[] CategoryIds { get; }
        }
    }
}