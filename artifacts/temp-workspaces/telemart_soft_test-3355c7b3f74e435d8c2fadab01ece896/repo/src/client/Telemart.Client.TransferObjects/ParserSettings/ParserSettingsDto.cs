using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSettings
{
    public sealed record ParserSettingsDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("auto_recognize")]
        public bool AutoRecognize { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("price_rrp_lifetime")]
        public int PriceRrpLifetime { get; set; }

        [JsonProperty("price_retail_lifetime")]
        public int PriceRetailLifetime { get; set; }

        [JsonProperty("price_wholesale_lifetime")]
        public int PriceWholesaleLifetime { get; set; }

        [JsonProperty("parse_all_brands")]
        public bool ParseAllBrands { get; set; }

        [JsonProperty("parse_inactive_categories")]
        public bool ParseInactiveCategories { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("parser_settings_file")]
        public ParserSettingsFileDto ParserSettingsFile { get; set; }

        [JsonProperty("availabilities")]
        public List<ParserSettingsAvailabilityDto> Availabilities { get; set; }

        [JsonProperty("categories")]
        public List<ParserSettingsCategoryDto> Categories { get; set; }
    }
}