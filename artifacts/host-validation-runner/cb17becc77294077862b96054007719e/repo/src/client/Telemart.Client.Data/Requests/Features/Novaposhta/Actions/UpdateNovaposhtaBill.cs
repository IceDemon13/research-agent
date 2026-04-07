using System.Net;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.NovaposhtaBill;

namespace Telemart.Client.Data.Requests.Features.Novaposhta.Actions
{
    public class UpdateNovaposhtaBill : CreateEntityResultRequestBase<object, NovaposhtaBillSaveDto>
    {
        public UpdateNovaposhtaBill(NovaposhtaBillSaveDto dto)
            : base(dto, ApiResources.Novaposhta, "actions", "update_bill")
        {
            SuccessStatusCode = HttpStatusCode.OK;
        }
    }
}