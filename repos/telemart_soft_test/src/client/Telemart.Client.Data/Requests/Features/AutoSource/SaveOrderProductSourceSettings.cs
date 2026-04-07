using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AutoSource;

namespace Telemart.Client.Data.Requests.Features.AutoSource
{
    public sealed class SaveOrderProductSourceSettings : CallActionWithBodyRequestResultBase<object, OrderProductSourceSettingsSaveDto>
    {
        public SaveOrderProductSourceSettings(IReadOnlyCollection<OrderProductSourceSettingSaveDto> dtos)
            : base(new OrderProductSourceSettingsSaveDto(dtos), $"{ApiResources.AutoSource}/values", "save_order_product_source_settings")
        {
        }
    }
}