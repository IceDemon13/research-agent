using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.WorkPlace
{
    public sealed class QueryWorkPlace : QueryEntityRequestBase<WorkPlaceDto>
    {
        public QueryWorkPlace(string uniqueDeviceId)
            : base(ApiResources.WorkPlaces, uniqueDeviceId)
        {
        }
    }
}