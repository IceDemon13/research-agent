using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Products.Content
{
    public class DeleteFeatureProductValue : DeleteEntityResultRequestBase<object>
    {
        public DeleteFeatureProductValue(int featureValueId)
            : base(ApiResources.Features, "products", "values", featureValueId)
        {
        }
    }
}