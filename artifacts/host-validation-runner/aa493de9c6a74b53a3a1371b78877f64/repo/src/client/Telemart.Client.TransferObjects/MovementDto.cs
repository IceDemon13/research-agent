using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class MovementDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("warehouse_from_id")]
        public int WarehouseFromId { get; init; }

        [JsonProperty("warehouse_to_id")]
        public int WarehouseToId { get; init; }

        [JsonProperty("date_out")]
        public DateTime DateOut { get; init; }

        [JsonProperty("date_in")]
        public DateTime DateIn { get; init; }

        [JsonProperty("date_departure")]
        public DateTime DateDeparture { get; init; }

        [JsonProperty("date_arrive")]
        public DateTime DateArrive { get; init; }

        [JsonProperty("state_id")]
        public int StateId { get; init; }

        [JsonProperty("carry_id")]
        public int? CarryId { get; init; }

        [JsonProperty("places")]
        public int? Places { get; init; }

        [JsonProperty("track_number")]
        public string TrackNumber { get; init; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("sent_on")]
        public DateTime? SentOn { get; init; }

        [JsonProperty("sent_by")]
        public int? SentBy { get; init; }

        [JsonProperty("received_on")]
        public DateTime? ReceivedOn { get; init; }

        [JsonProperty("arrived_on")]
        public DateTime? ArrivedOn { get; init; }

        [JsonProperty("received_by")]
        public int? ReceivedBy { get; init; }

        [JsonProperty("source_current_date_x")]
        public bool? SourceCurrentDateX { get; init; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; init; }

        [JsonProperty("movement_products")]
        public MovementProductDto[] MovementProducts { get; init; }

        [JsonProperty("assembly_service_products")]
        public IReadOnlyCollection<MovementAssemblyServiceProductDto> AssemblyServiceProducts { get; init; }

        [JsonProperty("additional_service_products")]
        public IReadOnlyCollection<MovementAdditionalServiceProductDto> AdditionalServiceProducts { get; init; }

        [JsonProperty("assembled_computers")]
        public IReadOnlyCollection<MovementAssembledComputerDto> AssembledComputers { get; init; }

        [JsonProperty("order_ids")]
        public int[] OrderIds { get; init; }

        [JsonProperty("purpose_ids")]
        public List<int> PurposeIds { get; init; }

        [JsonProperty("np_courier_call_barcode")]
        public string NpCourierCallBarcode { get; init; }

        [JsonProperty("np_courier_call_interval")]
        public string NpCourierCallInterval { get; init; }
    }
}