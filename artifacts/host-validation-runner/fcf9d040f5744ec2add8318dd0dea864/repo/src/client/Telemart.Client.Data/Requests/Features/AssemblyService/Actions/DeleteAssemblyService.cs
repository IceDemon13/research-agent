using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public class DeleteAssemblyService : DeleteEntityResultRequestBase<object>
    {
        public DeleteAssemblyService(int id)
            : base(ApiResources.AssemblyService, id)
        {
        }
    }
}