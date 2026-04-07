using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AutoSource;

namespace Telemart.Client.Data.Requests.Features.AutoSource
{
    public sealed class SaveAutoSourceSettings : CallActionWithBodyRequestResultBase<object, AutoSourceSettingsSaveDto>
    {
        public SaveAutoSourceSettings(AutoSourceSettingsSaveDto dto)
            : base(dto, $"{ApiResources.AutoSource}/values", "save")
        {
        }
    }
}