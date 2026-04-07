using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QuerySupplierProducts : CallActionWithBodyRequestBase<IReadOnlyCollection<SupplierProductDto>, QuerySupplierProductsDto>
    {
        public QuerySupplierProducts(QuerySupplierProductsDto dto)
            : base(dto, ApiResources.Products, "supplier_products")
        {
        }
    }
}