using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Products.Actions
{
    public sealed class CleanProductFromCash : CallEntityActionRequestResultBase<object>
    {
        public CleanProductFromCash(int productId)
            : base(productId, ApiResources.Products, "clean_cash")
        {
        }
    }
}