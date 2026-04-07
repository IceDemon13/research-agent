using System.Collections.Generic;
using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public class SaveProductVideos : CreateEntityRequestBase<object, IReadOnlyCollection<ProductVideoDto>>
    {
        public SaveProductVideos(IReadOnlyCollection<ProductVideoDto> dto)
            : base(dto, ApiResources.Products, "videos")
        {
            SuccessStatusCode = HttpStatusCode.NoContent;
        }
    }
}