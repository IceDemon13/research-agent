using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Showcase
{
    public class QueryShowcaseCategoriesLeftoversQuantity : QueryRequestBase<object>
    {
        public QueryShowcaseCategoriesLeftoversQuantity(int categoryId, int warehouseId)
            : base(ApiResources.Showcases, "leftovers_quantity")
        {
            UrlParameters = new (string Name, object Value)[]
            {
                ("categoryId", categoryId),
                ("warehouseId", warehouseId)
            };
        }
    }
}