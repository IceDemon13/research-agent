using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public sealed class QueryCarryPlaces : QueryEntityRequestBase<List<DeliveryServicePlaceDto>>
    {
        public QueryCarryPlaces(object id, int cityId)
            : base(ApiResources.Carries, id, "places")
        {
            UrlParameters = new (string Name, object Value)[] { ("city", cityId) };
        }
    }
}