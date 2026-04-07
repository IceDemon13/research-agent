using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Base;

namespace Telemart.Client.TransferObjects
{
    public class LegalEntityDto : TrackableDtoBase<int>
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("report_name")]
        public string ReportName { get; set; }

        [JsonProperty("white")]
        public bool White { get; set; }

        [JsonProperty("edrpou")]
        public string Edrpou { get; set; }

        [JsonProperty("fiscal_cashbox_id")]
        public int? FiscalCashboxId { get; init; }
    }
}