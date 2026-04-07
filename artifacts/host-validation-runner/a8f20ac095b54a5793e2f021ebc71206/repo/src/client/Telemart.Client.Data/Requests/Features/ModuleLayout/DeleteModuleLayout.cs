using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ModuleLayout
{
    public sealed class DeleteModuleLayout : DeleteEntityRequestBase
    {
        public DeleteModuleLayout(int moduleLayoutId)
            : base(ApiResources.ModuleLayouts, moduleLayoutId)
        {
        }
    }
}