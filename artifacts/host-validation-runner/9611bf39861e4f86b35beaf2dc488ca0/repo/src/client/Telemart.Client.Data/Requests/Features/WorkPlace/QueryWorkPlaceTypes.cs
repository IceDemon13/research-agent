using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.WorkPlace
{
    public sealed class QueryWorkPlaceTypes : QueryEntitiesRequestBase<WorkPlaceTypeDto>
    {
        public QueryWorkPlaceTypes()
            : base($"{ApiResources.WorkPlaces}/work_place_types")
        {
        }
    }
}