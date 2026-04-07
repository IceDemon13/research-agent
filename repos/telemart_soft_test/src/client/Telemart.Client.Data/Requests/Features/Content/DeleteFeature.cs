using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Content
{
    public sealed class DeleteFeature : DeleteEntityRequestBase
    {
        public DeleteFeature(int featureId)
            : base(ApiResources.Features, featureId.ToString())
        {
        }
    }
}