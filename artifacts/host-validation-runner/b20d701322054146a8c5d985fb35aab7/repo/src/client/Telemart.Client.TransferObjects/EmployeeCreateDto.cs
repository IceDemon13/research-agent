using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class EmployeeCreateDto
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("surname")]
        public string Surname { get; set; }

        [JsonProperty("name_translit")]
        public string NameTranslit { get; set; }

        [JsonProperty("surname_translit")]
        public string SurnameTranslit { get; set; }

        [JsonProperty("phone1")]
        public string Phone1 { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("skype")]
        public string Skype { get; set; }

        [JsonProperty("telegram")]
        public string Telegram { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("department_id")]
        public int DepartmentId { get; set; }

        [JsonProperty("city_id")]
        public int CityId { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("password_confirmation")]
        public string PasswordConfirmation { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("create_account_1C")]
        public bool CreateAccount1C { get; set; }

        [JsonProperty("create_account_telemart")]
        public bool CreateAccountTelemart { get; set; }

        [JsonProperty("create_site_customer")]
        public bool CreateSiteCustomer { get; set; }

        [JsonProperty("create_cashbox")]
        public bool CreateCashbox { get; set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; set; }
    }
}