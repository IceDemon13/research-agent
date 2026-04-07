using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssembledComputer
{
    public sealed class QueryAssembledComputers : QueryEntitiesRequestBase<AssembledComputerDto>
    {
        public QueryAssembledComputers(IFilteringItem filter)
            : base(filter, ApiResources.AssembledComputers)
        {
        }
    }
}