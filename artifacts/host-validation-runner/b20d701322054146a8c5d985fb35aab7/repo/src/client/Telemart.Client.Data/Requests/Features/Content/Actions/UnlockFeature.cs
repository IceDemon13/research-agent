using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Content.Actions
{
    public sealed class UnlockFeature : UnlockRequestBase<FeatureFullDto>
    {
        public UnlockFeature(int id, bool force = false)
            : base(force, ApiResources.Features, id)
        {
        }
    }
}
