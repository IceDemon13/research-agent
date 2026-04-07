using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AutoSource
{
    public sealed class OrderProductSourceSettingsSaveDto
    {
        public OrderProductSourceSettingsSaveDto(IReadOnlyCollection<OrderProductSourceSettingSaveDto> orderProductSourceSettings)
        {
            OrderProductSourceSettings = orderProductSourceSettings;
        }

        [JsonProperty("order_product_source_settings")]
        public IReadOnlyCollection<OrderProductSourceSettingSaveDto> OrderProductSourceSettings { get; }
    }
}