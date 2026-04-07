using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Carry;

namespace Telemart.Client.Data.Requests.Features.Carry
{
    public class QueryCarryPrices : QueryEntityRequestBase<List<CarryPriceDto>>
    {
        public QueryCarryPrices(int carryId)
            : base(ApiResources.Carries, carryId, ApiResources.CarryPrices)
        {
        }
    }
}