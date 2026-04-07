using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ServiceCenterSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("city_id")]
        public int CityId { get; set; }

        [JsonProperty("supplier_id")]
        public int? SupplierId { get; set; }

        [JsonProperty("repair_confirm_type_id")]
        public int RepairConfirmTypeId { get; set; }

        [JsonProperty("invoices_per_day")]
        public int InvoicesPerDay { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("skype")]
        public string Skype { get; set; }

        [JsonProperty("icq")]
        public string Icq { get; set; }

        [JsonProperty("recipient_fio")]
        public string RecipientFio { get; set; }

        [JsonProperty("recipient_phone")]
        public string RecipientPhone { get; set; }

        [JsonProperty("edrpou")]
        public string Edrpou { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("regulations")]
        public string Regulations { get; set; }

        [JsonProperty("np_warehouse_ref")]
        public Guid? NpWarehouseRef { get; set; }

        [JsonProperty("np_postbox_ref")]
        public Guid? NpPostBoxRef { get; set; }

        [JsonProperty("np_street_ref")]
        public Guid? NpStreetRef { get; set; }

        [JsonProperty("np_house")]
        public string NpHouse { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}