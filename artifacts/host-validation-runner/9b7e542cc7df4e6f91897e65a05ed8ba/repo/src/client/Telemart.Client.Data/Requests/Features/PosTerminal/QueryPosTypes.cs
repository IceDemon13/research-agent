using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PosTerminal;

namespace Telemart.Client.Data.Requests.Features.PosTerminal
{
    public sealed class QueryPosTypes : QueryEntitiesRequestBase<PosTypeDto>
    {
        public QueryPosTypes()
            : base($"{ApiResources.Pos}/types")
        {
        }
    }
}