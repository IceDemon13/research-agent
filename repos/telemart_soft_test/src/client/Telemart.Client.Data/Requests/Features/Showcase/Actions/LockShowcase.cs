using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase.Actions
{
    public class LockShowcase : LockRequestBase<ShowcaseDto>
    {
        public LockShowcase(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Showcases, id)
        {
        }
    }
}
