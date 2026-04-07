using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AssemblyService
{
    public class QueryAssemblyTestResults : QueryEntitiesRequestBase<AssemblyTestResultDto>
    {
        public QueryAssemblyTestResults(IFilteringItem filter)
            : base(filter, $"{ApiResources.AssemblyService}/test_results")
        {
        }
    }
}