using Newtonsoft.Json;

namespace Telemart.Client.Common.Settings.Equipment
{
    public class PosSettingsInfo
    {
        public PosSettingsInfo(string ip, string merchant, int? cashboxId, PosType? type)
        {
            Ip = ip;
            Merchant = merchant;
            CashboxId = cashboxId;
            Type = type;
        }

        public PosSettingsInfo()
        {
            Type = PosType.Ingenico;
        }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("merchant")]
        public string Merchant { get; set; }

        [JsonProperty("cashbox_id")]
        public int? CashboxId { get; set; }

        [JsonProperty("type")]
        public PosType? Type { get; set; }

        public bool PosIsValid()
        {
            return !string.IsNullOrWhiteSpace(Ip)
                   && !string.IsNullOrWhiteSpace(Merchant)
                   && CashboxId != null;
        }

        public void Clean()
        {
            Ip = Merchant = null;
            CashboxId = null;
            Type = null;
        }
    }
}