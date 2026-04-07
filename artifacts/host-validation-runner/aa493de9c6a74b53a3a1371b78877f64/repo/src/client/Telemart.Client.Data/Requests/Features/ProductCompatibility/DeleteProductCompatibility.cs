using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.ProductCompatibility
{
    public class DeleteProductCompatibility : DeleteEntityResultRequestBase<object>
    {
        public DeleteProductCompatibility(int id)
            : base(ApiResources.ProductCompatibilities, id)
        {
        }
    }
}