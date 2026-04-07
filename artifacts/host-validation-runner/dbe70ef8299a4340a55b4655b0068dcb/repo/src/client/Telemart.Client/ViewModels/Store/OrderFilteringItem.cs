using System;
using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class OrderFilteringItem : PagingFilteringItem
    {
        // Do not remove setters from filtering item properties! They are settered via reflection
        public OrderFilteringItem(string fio, List<int> subdivisions)
        {
            Fio = fio;
            Subdivisions = subdivisions;
            FromClient = true;
        }

        public OrderFilteringItem()
        {
            FromClient = true;
        }

        [FilteringItemProperty("cellphone")]
        public string Cellphone { get; set; }

        [FilteringItemProperty("pack_list_ids")]
        public string PackListIds { get; set; }

        [FilteringItemProperty("email")]
        public string Email { get; set; }

        [FilteringItemProperty("track_numbers")]
        public string TrackNumbers { get; set; }

        [FilteringItemProperty("fio")]
        public string Fio { get; set; }

        [FilteringItemProperty("pko")]
        public bool? PkoBool { get; set; }

        [FilteringItemProperty("rt")]
        public bool? RtBool { get; set; }

        [FilteringItemProperty("any_new_calls")]
        public bool? AnyNewCalls { get; set; }

        [FilteringItemProperty("received_by_customer")]
        public bool? ReceivedByCustomer { get; set; }

        [FilteringItemProperty("change_order")]
        public bool? ChangeOrder { get; set; }

        [FilteringItemProperty("any_assembled_computer_rules")]
        public bool? AnyAssembledComputerRules { get; set; }

        [FilteringItemProperty("contractor")]
        public string Contractor { get; set; }

        [FilteringItemProperty("order_closed_after")]
        public DateTime? OrderClosedAfter { get; set; }

        [FilteringItemProperty("order_closed_before")]
        public DateTime? OrderClosedBefore { get; set; }

        [FilteringItemProperty("order_completed_on_after")]
        public DateTime? OrderCompletedOnAfter { get; set; }

        [FilteringItemProperty("order_completed_on_before")]
        public DateTime? OrderCompletedOnBefore { get; set; }

        [FilteringItemProperty("order_created_after")]
        public DateTime? OrderCreatedAfter { get; set; }

        [FilteringItemProperty("order_created_before")]
        public DateTime? OrderCreatedBefore { get; set; }

        [FilteringItemProperty("order_numbers")]
        public string OrderNumbers { get; set; }

        [FilteringItemProperty("external_order_numbers")]
        public string ExternalOrderNumbers { get; set; }

        [FilteringItemProperty("include_customer_state_text")]
        public bool IncludeCustomerStateText { get; set; }

        [FilteringItemProperty("include_preorders")]
        public bool IncludePreorders { get; set; }

        [FilteringItemProperty("service_request_numbers")]
        public string ServiceRequestNumbers { get; set; }

        [FilteringItemProperty("payments")]
        public List<int> Payments { get; set; }

        [FilteringItemProperty("subdivisions")]
        public List<int> Subdivisions { get; set; }

        [FilteringItemProperty("cities")]
        public List<int> Cities { get; set; }

        [FilteringItemProperty("contractors")]
        public List<int> Contractors { get; set; }

        [FilteringItemProperty("warehouses")]
        public List<int> Warehouses { get; set; }

        [FilteringItemProperty("carries")]
        public List<int> Carries { get; set; }

        [FilteringItemProperty("orderStatuses")]
        public List<int> OrderStatuses { get; set; }

        [FilteringItemProperty("product")]
        public string Product { get; set; }

        [FilteringItemProperty("product_ids")]
        public int[] ProductIds { get; set; }

        [FilteringItemProperty("manager")]
        public List<int> Managers { get; set; }

        [FilteringItemProperty("responsible")]
        public List<int> ResponsibleEmployees { get; set; }

        [FilteringItemProperty("created_by")]
        public List<int> CreatedByEmployees { get; set; }

        [FilteringItemProperty("legal_entities")]
        public List<int> LegalEntities { get; set; }

        [FilteringItemProperty("order_sources")]
        public List<int> OrderSources { get; set; }

        [FilteringItemProperty("pack_list_info")]
        public bool? PackListInfo { get; set; }

        [FilteringItemProperty("from_client")]
        public bool FromClient { get; }

        [FilteringItemProperty("canceled_from_site")]
        public bool? CanceledFromSite { get; set; }

        [FilteringItemProperty("completed_on_fiscal_registrar")]
        public bool? CompletedOnFiscalRegistrar { get; init; }
    }
}