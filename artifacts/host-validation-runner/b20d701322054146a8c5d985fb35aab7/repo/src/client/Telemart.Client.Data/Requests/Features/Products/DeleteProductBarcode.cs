using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class DeleteProductBarcode : DeleteEntityRequestBase
    {
        public DeleteProductBarcode(string barcode)
            : base(ApiResources.Products, "barcodes", barcode)
        {
        }
    }
}