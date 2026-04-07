using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct
{
    public sealed class QueryServiceProduct : QueryEntityRequestBase<ServiceProductDto>
    {
        public QueryServiceProduct(int id)
            : base(ApiResources.ServiceProducts, id)
        {
        }
    }
}