using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct
{
    public sealed class UpdateServiceProductComment : UpdateCommentBase<ServiceProductDto>
    {
        public UpdateServiceProductComment(int id, string comment)
            : base(id, ApiResources.ServiceProducts, comment)
        {
        }
    }
}