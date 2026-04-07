using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Data.Requests.Features.City.Actions
{
    public class UnlockCity : UnlockRequestBase<CityDto>
    {
        public UnlockCity(int id, bool force = false)
            : base(force, ApiResources.Cities, id)
        {
        }
    }
}