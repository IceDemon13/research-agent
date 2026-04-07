using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Products.Content
{
    public sealed class CreateFeatureProductValue : CreateEntityResultRequestBase<FeatureValueExDto, FeatureValueCreateDto>
    {
        public CreateFeatureProductValue(FeatureValueCreateDto dto)
            : base(dto, $"{ApiResources.Features}/products/values")
        {
        }
    }
}