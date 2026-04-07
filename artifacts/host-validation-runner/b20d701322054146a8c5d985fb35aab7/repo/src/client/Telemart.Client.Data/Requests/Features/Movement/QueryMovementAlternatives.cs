using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Movement
{
    public sealed class QueryMovementAlternatives : QueryEntityRequestBase<List<ProductAlternativeDto>>
    {
        public QueryMovementAlternatives(int id)
            : base(ApiResources.Movements, id, "products", "alternatives")
        {
        }
    }
}