using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public class DeleteNpBill : DeleteEntityRequestBase
    {
        public DeleteNpBill(int id)
            : base(ApiResources.Novaposhta, "bill", id.ToString())
        {
        }
    }
}