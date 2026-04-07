using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products.Catalog
{
    public sealed class UpdateProductsCatalog : CallActionWithBodyRequestBase<ProductsCatalogSaveResponse, ProductsCatalogSaveRequest>
    {
        public UpdateProductsCatalog(ProductsCatalogSaveRequest request)
            : base(request, "products/catalog", "save")
        {
        }
    }
}