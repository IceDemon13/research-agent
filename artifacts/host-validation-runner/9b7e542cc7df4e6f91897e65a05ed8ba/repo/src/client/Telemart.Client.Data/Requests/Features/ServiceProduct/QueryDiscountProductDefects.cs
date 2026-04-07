using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct
{
    public sealed class QueryDiscountProductDefects : QueryEntitiesRequestBase<DiscountProductDefectDto>
    {
        public QueryDiscountProductDefects()
            : base(ApiResources.ServiceProducts, "discount_product_defect")
        {
        }
    }
}