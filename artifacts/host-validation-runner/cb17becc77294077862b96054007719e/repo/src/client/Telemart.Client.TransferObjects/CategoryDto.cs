using Newtonsoft.Json;
using Telemart.Client.TransferObjects.Base;
using Telemart.Common.Localization;

namespace Telemart.Client.TransferObjects
{
    public class CategoryDto : TrackableDtoBase<int>, ILocalіzableEntity
    {
        [JsonProperty("active")]
        public double Active { get; set; }

        [JsonProperty("is_parent")]
        public bool IsParent { get; set; }

        [JsonProperty("left")]
        public int Left { get; set; }

        [JsonProperty("right")]
        public int Right { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("parent_level")]
        public int ParentLevel { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("name_full")]
        public string NameFull { get; set; }

        [JsonProperty("manufacturer")]
        public string Manufactor { get; set; }

        [JsonProperty("prefix_rus")]
        public string PrefixRus { get; set; }

        [JsonProperty("prefix_ukr")]
        public string PrefixUkr { get; set; }

        [JsonProperty("prefix_en")]
        public string PrefixEn { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("parent_id")]
        public int ParentId { get; set; }

        [JsonProperty("parent_name")]
        public string ParentName { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("ym_hid")]
        public int YandexMarketHid { get; set; }

        [JsonProperty("type_id")]
        public int? TypeId { get; set; }

        [JsonProperty("use_in_trade_in")]
        public bool UseInTradeIn { get; set; }

        [JsonIgnore]
        public string FullName => string.IsNullOrWhiteSpace(NameFull)
            ? Name
            : NameFull;
    }
}