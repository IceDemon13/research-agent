using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryRecognizeSupplierProducts : CallActionWithBodyRequestBase<IReadOnlyCollection<SupplierProductDto>, QueryRecognizeSupplierProductsDto>
    {
        public QueryRecognizeSupplierProducts(QueryRecognizeSupplierProductsDto dto)
            : base(dto, ApiResources.Products, "recognize_supplier_products")
        {
        }
    }
}