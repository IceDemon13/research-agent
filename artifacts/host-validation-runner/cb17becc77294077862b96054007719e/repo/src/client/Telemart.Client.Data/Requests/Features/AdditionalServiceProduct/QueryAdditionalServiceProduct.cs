using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct
{
    public sealed class QueryAdditionalServiceProduct : QueryEntityRequestBase<AdditionalServiceProductDto>
    {
        public QueryAdditionalServiceProduct(object id)
            : base(ApiResources.AdditionalServicesProducts, id)
        {
        }
    }
}