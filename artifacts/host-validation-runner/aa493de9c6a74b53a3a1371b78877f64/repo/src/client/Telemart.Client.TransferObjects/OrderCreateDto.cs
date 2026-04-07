using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCreateDto : OrderSaveDto
    {
        [JsonProperty("client_id")]
        public int ClientId { get; set; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; set; }

        [JsonProperty("fill_sources")]
        public bool FillSources { get; set; }

        [JsonProperty("work_place_id")]
        public int? WorkPlaceId { get; set; }

        [JsonProperty("notify_by_sms")]
        public bool NotifyBySms { get; set; }

        [JsonProperty("ignore_recalculating_price_out_after_bonus_apply")]
        public bool IgnoreRecalculatingPriceOutAfterBonusApply { get; set; }

        [JsonProperty("bonuses")]
        public List<OrderBonusSaveDto> Bonuses { get; set; } = new List<OrderBonusSaveDto>();
    }
}