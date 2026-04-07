using System;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Data.Requests.Features.City
{
    public sealed class QueryCities : QueryEntitiesPagedRequestBase<CityDto>, IChunkSupport
    {
        public QueryCities()
            : base(ApiResources.Cities)
        {
        }

        public QueryCities(DateTime? modifiedOnAfter)
            : base(new ModifiedOnAfterFilteringItem { ModifiedOnAfter = modifiedOnAfter }, ApiResources.Cities)
        {
        }
    }
}