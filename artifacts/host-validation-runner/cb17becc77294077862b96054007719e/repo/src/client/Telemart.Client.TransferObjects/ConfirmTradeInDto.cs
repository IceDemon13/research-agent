using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ConfirmTradeInDto
    {
        public ConfirmTradeInDto(int serviceRequestId, int bonusAmount)
        {
            ServiceRequestId = serviceRequestId;
            BonusAmount = bonusAmount;
        }

        [JsonProperty("id_service_request")]
        public int ServiceRequestId { get; set; }

        [JsonProperty("bonus_amount")]
        public int BonusAmount { get; set; }
    }
}