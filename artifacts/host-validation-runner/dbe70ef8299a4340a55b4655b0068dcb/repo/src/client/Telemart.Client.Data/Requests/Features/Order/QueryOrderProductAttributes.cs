using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class QueryOrderProductAttributes : QueryEntityRequestBase<List<ProductAttributesDto>>
    {
        public QueryOrderProductAttributes(int id)
            : base(ApiResources.Orders, id, "products", "attributes")
        {
        }
    }
}