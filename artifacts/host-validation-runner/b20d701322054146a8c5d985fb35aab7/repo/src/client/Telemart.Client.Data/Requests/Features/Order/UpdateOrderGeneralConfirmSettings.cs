using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderGeneralConfirmSettings : UpdateEntityRequestBase<Result<OrderGeneralConfirmSettingDto[]>, UpdateOrderGeneralConfirmSettings.SettingsSaveDto>
    {
        public UpdateOrderGeneralConfirmSettings(IReadOnlyCollection<SaveOrderGeneralConfirmSettingDto> settings)
            : base(new SettingsSaveDto(settings), ApiResources.Orders, "general_confirm_settings")
        {
        }

        public sealed class SettingsSaveDto
        {
            public SettingsSaveDto(IReadOnlyCollection<SaveOrderGeneralConfirmSettingDto> settings)
            {
                Settings = settings;
            }

            [JsonProperty("settings")]
            public IReadOnlyCollection<SaveOrderGeneralConfirmSettingDto> Settings { get; }
        }
    }
}