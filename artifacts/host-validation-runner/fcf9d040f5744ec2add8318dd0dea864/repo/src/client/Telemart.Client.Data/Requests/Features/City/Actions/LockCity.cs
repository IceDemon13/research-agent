using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Data.Requests.Features.City.Actions
{
    public class LockCity : LockRequestBase<CityDto>
    {
        public LockCity(int id, bool allowLockedByMe = true, bool checkPermissions = false)
            : base(allowLockedByMe, checkPermissions, ApiResources.Cities, id)
        {
        }
    }
}