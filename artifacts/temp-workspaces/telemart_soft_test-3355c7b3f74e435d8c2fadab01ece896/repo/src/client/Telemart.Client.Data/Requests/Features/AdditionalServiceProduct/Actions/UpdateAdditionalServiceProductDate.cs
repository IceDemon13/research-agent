using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.AdditionalServiceProduct.Actions
{
    public sealed class UpdateAdditionalServiceProductDate : CallEntityActionWithBodyRequestResultBase<AdditionalServiceProductDto, UpdateAdditionalServiceProductDate.UpdateAdditionalServiceProductDateDto>
    {
        public UpdateAdditionalServiceProductDate(int additionalServiceProductId, DateTime date, int? orderStateChangeReasonId, string comment)
            : base(additionalServiceProductId, new UpdateAdditionalServiceProductDateDto(additionalServiceProductId, date, orderStateChangeReasonId, comment), ApiResources.AdditionalServicesProducts, "update_date")
        {
        }

        public class UpdateAdditionalServiceProductDateDto
        {
            public UpdateAdditionalServiceProductDateDto(int id, DateTime dateTime, int? orderStateChangeReasonId, string comment)
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