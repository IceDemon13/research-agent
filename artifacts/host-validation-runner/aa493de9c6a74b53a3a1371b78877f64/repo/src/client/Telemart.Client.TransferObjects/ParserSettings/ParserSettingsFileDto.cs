using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSettings
{
    public sealed record ParserSettingsFileDto
    {
        [JsonProperty("start_line")]
        public int StartLine { get; init; }

        [JsonProperty("start_word")]
        public string StartWord { get; init; }

        [JsonProperty("stop_word")]
        public string StopWord { get; init; }

        [JsonProperty("sheets_type_id")]
        public int SheetsTypeId { get; init; }

        [JsonProperty("sheets_recognize_type_id")]
        public int SheetsRecognizeTypeId { get; init; }

        [JsonProperty("sheets_recognize_pattern")]
        public string SheetsRecognizePattern { get; init; }

        [JsonProperty("supplier_warehouse_id")]
        public int? SupplierWarehouseId { get; init; }

        [JsonProperty("file_column")]
        public ParserSettingsFileColumnDto FileColumn { get; init; }
    }
}