using Newtonsoft.Json;
using Telemart.Client.FiscalRegistrar;

namespace Telemart.Client.Common.Settings.Equipment
{
    public sealed class FiscalRegistrarSettingsInfo
    {
        public FiscalRegistrarSettingsInfo(string ip, string user, string password, string type, int? cashboxId)
        {
            Ip = ip;
            User = user;
            Type = type;
            Password = password;
            CashboxId = cashboxId;
        }

        public FiscalRegistrarSettingsInfo()
        {
        }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("cashbox_id")]
        public int? CashboxId { get; set; }

        public bool FiscalRegistrarIsValid()
        {
            return !string.IsNullOrEmpty(Type)
                   && (Type == FiscalRegistrarType.Software.Name
                       || (!string.IsNullOrWhiteSpace(Ip)
                           && !string.IsNullOrWhiteSpace(User)
                           && !string.IsNullOrWhiteSpace(Password)
                           && CashboxId != null));
        }

        public void Clean()
        {
            Ip = User = Password = Type = null;
            CashboxId = null;
        }
    }
}