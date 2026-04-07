using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService
{
    public class QueryAssemblyService : QueryEntityRequestBase<AssemblyServiceDto>
    {
        public QueryAssemblyService(object id)
            : base(ApiResources.AssemblyService, id)
        {
        }
    }
}