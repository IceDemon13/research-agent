using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.ServiceRepair
{
    public sealed class UpdateServiceRepair : UpdateEntityResultRequestBase<ServiceRepairDto, ServiceRepairSaveDto>
    {
        public UpdateServiceRepair(int id, int? serviceCenterId, string defect, string comment)
            : base(new ServiceRepairSaveDto(id, serviceCenterId, defect, comment), ApiResources.ServiceRepairs, id)
        {
        }
    }
}