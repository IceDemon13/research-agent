using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.AdditionalServiceProduct
{
    public class AdditionalServiceProductsFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("ids")]
        public string Ids { get; set; }

        [FilteringItemProperty("order_ids")]
        public string OrderIds { get; set; }

        [FilteringItemProperty("warehouse_ids")]
        public string WarehouseIds { get; set; }

        [FilteringItemProperty("order_warehouse_ids")]
        public string OrderWarehouseIds { get; set; }

        [FilteringItemProperty("state_ids")]
        public string StateIds { get; set; }

        [FilteringItemProperty("movement_ids")]
        public string MovementIds { get; set; }

        [FilteringItemProperty("order_state_ids")]
        public string OrderStateIds { get; set; }

        [FilteringItemProperty("control_in_movements")]
        public bool? ControlInMovements { get; set; }

        [FilteringItemProperty("order_delivery_time_to_before")]
        public DateTime? OrderDeliveryTimeToBefore { get; set; }

        [FilteringItemProperty("order_delivery_time_to_after")]
        public DateTime? OrderDeliveryTimeToAfter { get; set; }

        [FilteringItemProperty("employee_ids")]
        public string EmployeeIds { get; set; }

        [FilteringItemProperty("parent_ids")]
        public string ParentIds { get; set; }
    }
}