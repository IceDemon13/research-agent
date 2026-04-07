using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ModuleLayout
{
    public sealed class QueryModuleLayouts : QueryEntitiesRequestBase<ModuleLayoutDto>
    {
        public QueryModuleLayouts(int moduleId)
            : base($"{ApiResources.ModuleLayouts}/module/{moduleId}")
        {
        }
    }
}