using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct
{
    public sealed class UpdateAdditionalServiceProduct : UpdateEntityResultRequestBase<AdditionalServiceProductDto, AdditionalServiceProductSaveDto>
    {
        public UpdateAdditionalServiceProduct(int id, AdditionalServiceProductSaveDto saveDto)
            : base(saveDto, ApiResources.AdditionalServicesProducts, id)
        {
        }
    }
}