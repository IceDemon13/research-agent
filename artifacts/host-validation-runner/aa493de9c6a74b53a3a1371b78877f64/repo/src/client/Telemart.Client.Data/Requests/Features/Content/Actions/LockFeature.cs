using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Content.Actions
{
    public sealed class LockFeature : LockRequestBase<FeatureFullDto>
    {
        public LockFeature(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Features, id)
        {
        }
    }
}
