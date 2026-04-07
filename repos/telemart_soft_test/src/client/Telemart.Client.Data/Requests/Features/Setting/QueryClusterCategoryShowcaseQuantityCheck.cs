using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Setting
{
    public class QueryClusterCategoryShowcaseQuantityCheck : QueryEntityRequestBase<string>
    {
        public QueryClusterCategoryShowcaseQuantityCheck()
            : base(ApiResources.Settings, "cluster_category_showcase_quantity_check")
        {
        }
    }
}