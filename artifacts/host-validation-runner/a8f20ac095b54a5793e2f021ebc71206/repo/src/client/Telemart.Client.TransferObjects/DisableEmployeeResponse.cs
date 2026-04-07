using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class DisableEmployeeResponse
    {
        [JsonProperty("accounting_system_error")]
        public string AccountingSystemError { get; set; }

        [JsonProperty("bitrix_error")]
        public string BitrixError { get; set; }

        [JsonProperty("data")]
        public EmployeeRichDto Data { get; set; }

        [JsonProperty("local_windows_account_error")]
        public string LocalWindowsAccountError { get; set; }

        [JsonProperty("yandex_error")]
        public string YandexError { get; set; }

        public bool HasErrors()
        {
            return !string.IsNullOrWhiteSpace(YandexError) ||
                   !string.IsNullOrWhiteSpace(LocalWindowsAccountError) ||
                   !string.IsNullOrWhiteSpace(BitrixError) ||
                   !string.IsNullOrWhiteSpace(AccountingSystemError);
        }
    }
}