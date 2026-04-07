using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public class QueryClusterCategoryShowcaseAllowSetQuantity : QueryEntityRequestBase<string>
    {
        public QueryClusterCategoryShowcaseAllowSetQuantity()
            : base(ApiResources.Settings, "cluster_category_showcase_allow_set_quantity")
        {
        }
    }
}