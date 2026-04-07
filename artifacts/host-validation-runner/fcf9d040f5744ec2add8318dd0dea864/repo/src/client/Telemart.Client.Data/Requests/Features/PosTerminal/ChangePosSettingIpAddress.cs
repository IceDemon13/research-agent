using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.PosTerminal;

namespace Telemart.Client.Data.Requests.Features.PosTerminal
{
    public sealed class ChangePosSettingIpAddress : CallEntityActionWithBodyRequestResultBase<PosSettingsDto, ChangeIpAddressSettingDto>
    {
        public ChangePosSettingIpAddress(int posSettingId, ChangeIpAddressSettingDto dto)
            : base(posSettingId, dto, ApiResources.Pos, "change_ip_address")
        {
        }
    }
}