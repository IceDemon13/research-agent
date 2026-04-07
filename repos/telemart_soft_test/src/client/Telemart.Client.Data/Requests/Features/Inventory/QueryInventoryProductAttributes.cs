using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Inventory
{
    public sealed class QueryInventoryProductAttributes : QueryEntityRequestBase<List<ProductAttributesDto>>
    {
        public QueryInventoryProductAttributes(int id)
            : base(ApiResources.Inventories, id, "products", "attributes")
        {
        }
    }
}