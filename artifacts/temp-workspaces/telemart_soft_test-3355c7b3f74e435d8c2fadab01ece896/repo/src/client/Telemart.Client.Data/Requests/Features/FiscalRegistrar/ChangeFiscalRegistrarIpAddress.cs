using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Client.TransferObjects.PosTerminal;

namespace Telemart.Client.Data.Requests.Features.FiscalRegistrar
{
    public sealed class ChangeFiscalRegistrarIpAddress : CallEntityActionWithBodyRequestResultBase<FiscalRegistrarSettingsDto, ChangeIpAddressSettingDto>
    {
        public ChangeFiscalRegistrarIpAddress(int fiscalRegistrarId, ChangeIpAddressSettingDto dto)
            : base(fiscalRegistrarId, dto, ApiResources.Fiscal, "change_ip_address")
        {
        }
    }
}