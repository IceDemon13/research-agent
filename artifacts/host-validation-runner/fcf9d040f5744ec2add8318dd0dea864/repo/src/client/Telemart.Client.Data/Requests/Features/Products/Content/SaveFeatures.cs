using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Products.Content
{
    public sealed class SaveFeatures : CallActionWithBodyRequestResultBase<ProductContentDto[], IReadOnlyCollection<ProductContentSaveDto>>
    {
        public SaveFeatures(IReadOnlyCollection<ProductContentSaveDto> dtos)
            : base(dtos, $"{ApiResources.Features}/products", "save")
        {
        }
    }
}