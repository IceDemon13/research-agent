using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.SlotHosts
{
    public sealed class QueryAssemblySlotHosts : QueryEntitiesRequestBase<AssemblySlotHostDto>
    {
        public QueryAssemblySlotHosts()
            : base($"{ApiResources.SlotHosts}")
        {
        }
    }
}