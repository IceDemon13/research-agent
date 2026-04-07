using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order
{
    public sealed class UpdateOrderAutoConfirmSettings : UpdateEntityRequestBase<object, UpdateOrderAutoConfirmSettings.OrderAutoConfirmSettingSaveDto>
    {
        public UpdateOrderAutoConfirmSettings(IReadOnlyCollection<OrderAutoConfirmSettingDto> settings)
            : base(new OrderAutoConfirmSettingSaveDto(settings), ApiResources.Orders, ApiResources.AutoConfirmSettings)
        {
        }

        public class OrderAutoConfirmSettingSaveDto
        {
            public OrderAutoConfirmSettingSaveDto(IReadOnlyCollection<OrderAutoConfirmSettingDto> settings)
            {
                Settings = settings;
            }

            [JsonProperty("settings")]
            public IReadOnlyCollection<OrderAutoConfirmSettingDto> Settings { get; init; }
        }
    }
}