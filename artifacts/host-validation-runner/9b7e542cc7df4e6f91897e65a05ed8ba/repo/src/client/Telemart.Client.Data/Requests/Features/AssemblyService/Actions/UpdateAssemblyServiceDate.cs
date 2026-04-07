using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.AssemblyService;

namespace Telemart.Client.Data.Requests.Features.AssemblyService.Actions
{
    public sealed class UpdateAssemblyServiceDate : CallEntityActionWithBodyRequestResultBase<AssemblyServiceDto, UpdateAssemblyServiceDate.UpdateAssemblyServiceDateDto>
    {
        public UpdateAssemblyServiceDate(int assemblyServiceId, DateTime date, int? orderStateChangeReasonId, string comment)
            : base(assemblyServiceId, new UpdateAssemblyServiceDateDto(assemblyServiceId, date, orderStateChangeReasonId, comment), ApiResources.AssemblyService, "update_date")
        {
        }

        public class UpdateAssemblyServiceDateDto
        {
            public UpdateAssemblyServiceDateDto(int id, DateTime dateTime, int? orderStateChangeReasonId, string comment)
            {
                Id = id;
                DateTime = dateTime;
                OrderStateChangeReasonId = orderStateChangeReasonId;
                Comment = comment;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("date_time")]
            public DateTime DateTime { get; set; }

            [JsonProperty("order_state_change_reason_id")]
            public int? OrderStateChangeReasonId { get; }

            [JsonProperty("comment")]
            public string Comment { get; }
        }
    }
}