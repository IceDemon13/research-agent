using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct
{
    public sealed class UpdateServiceProduct : UpdateEntityResultRequestBase<ServiceProductDto, ServiceProductSaveDto>
    {
        public UpdateServiceProduct(int id, ServiceProductSaveDto dto)
            : base(dto, ApiResources.ServiceProducts, id)
        {
        }
    }
}
