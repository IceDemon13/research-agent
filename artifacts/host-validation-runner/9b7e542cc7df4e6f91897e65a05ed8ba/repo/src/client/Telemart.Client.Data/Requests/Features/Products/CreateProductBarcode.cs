using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public class CreateProductBarcode : CreateEntityResultRequestBase<ProductBarcodeDto, ProductBarcodeSaveDto>
    {
        public CreateProductBarcode(int productId, ProductBarcodeSaveDto dto)
            : base(dto, ApiResources.Products, productId, "barcodes")
        {
        }
    }
}