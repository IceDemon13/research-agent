using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair
{
    public sealed class UpdateServiceRepairComment : UpdateCommentBase<ServiceRepairDto>
    {
        public UpdateServiceRepairComment(int id, string comment)
            : base(id, ApiResources.ServiceRepairs, comment)
        {
        }
    }
}