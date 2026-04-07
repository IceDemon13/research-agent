using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public class ProcessOrderBonuses : CallEntityActionWithBodyRequestResultBase<OrderDto, ProcessOrderBonuses.OrderBonusTypeSaveDto>
    {
        public ProcessOrderBonuses(int orderId, int bonusTypeId, IReadOnlyCollection<OrderBonusSaveDto> bonuses)
            : base(orderId, new OrderBonusTypeSaveDto(orderId, bonusTypeId, bonuses), ApiResources.Orders, "bonuses")
        {
        }

        public class OrderBonusTypeSaveDto
        {
            public OrderBonusTypeSaveDto(int orderId, int bonusTypeId, IReadOnlyCollection<OrderBonusSaveDto> bonuses)
            {
                OrderId = orderId;
                BonusTypeId = bonusTypeId;
                Bonuses = bonuses;
            }

            [JsonProperty("order_id")]
            public int OrderId { get; set; }

            [JsonProperty("bonus_type_id")]
            public int BonusTypeId { get; set; }

            [JsonProperty("bonuses")]
            public IReadOnlyCollection<OrderBonusSaveDto> Bonuses { get; set; }
        }
    }
}
