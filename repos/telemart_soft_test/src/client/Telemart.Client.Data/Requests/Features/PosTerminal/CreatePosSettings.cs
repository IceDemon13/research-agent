using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.PosTerminal;

namespace Telemart.Client.Data.Requests.Features.PosTerminal
{
    public class CreatePosSettings : CreateEntityResultRequestBase<PosSettingsDto, CreatePosSettingsDto>
    {
        public CreatePosSettings(CreatePosSettingsDto createDto)
            : base(createDto, $"{ApiResources.Pos}/settings")
        {
        }
    }
}