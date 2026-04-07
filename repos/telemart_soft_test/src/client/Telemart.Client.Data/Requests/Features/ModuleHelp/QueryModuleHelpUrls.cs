using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ModuleHelp
{
    public sealed class QueryModuleHelpUrls : QueryEntitiesRequestBase<ModuleHelpUrlDto>
    {
        public QueryModuleHelpUrls()
            : base($"{ApiResources.ModuleHelp}/urls")
        {
        }
    }
}