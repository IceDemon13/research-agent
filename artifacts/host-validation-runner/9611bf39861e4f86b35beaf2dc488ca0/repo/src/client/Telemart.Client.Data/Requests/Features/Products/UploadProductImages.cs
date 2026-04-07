using System;
using System.Collections.Generic;
using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class UploadProductImages : CreateEntityRequestBase<object, ProductUploadImagesDto>
    {
        public UploadProductImages(int productId, IReadOnlyCollection<ImageDto> images)
            : base(new ProductUploadImagesDto { Id = productId, Images = images }, ApiResources.Products, productId, "images")
        {
            SuccessStatusCode = HttpStatusCode.NoContent;

            DefaultTimeout = TimeSpan.FromMinutes(6);
        }
    }
}