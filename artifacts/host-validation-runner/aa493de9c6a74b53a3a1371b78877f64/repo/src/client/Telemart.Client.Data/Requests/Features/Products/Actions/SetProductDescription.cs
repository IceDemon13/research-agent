using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products.Actions
{
    public sealed class SetProductDescription : CreateEntityRequestBase<ProductDescriptionSaveResponse, ProductDescriptionSaveRequest>
    {
        public SetProductDescription(ProductDescriptionSaveDto[] descriptions)
            : base(new ProductDescriptionSaveRequest(descriptions), ApiResources.Products, "actions", "set_description")
        {
            SuccessStatusCode = HttpStatusCode.OK;
        }
    }
}