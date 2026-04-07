using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Data.Requests.Features.Customer.Actions
{
    public class CustomerDeleteBonuses : CallEntityActionWithBodyRequestResultBase<CustomerDto, CustomerDeleteBonuses.DeleteBonusDto>
    {
        public CustomerDeleteBonuses(int customerId, int deleteQuantity, int bonusTypeId)
            : base(customerId, new DeleteBonusDto(customerId, deleteQuantity, bonusTypeId), ApiResources.Customers, "delete_bonuses")
        {
        }

        public class DeleteBonusDto
        {
            public DeleteBonusDto(int customerId, int deleteQuantity, int bonusTypeId)
            {
                CustomerId = customerId;
                DeleteQuantity = deleteQuantity;
                BonusTypeId = bonusTypeId;
            }

            [JsonProperty("customer_id")]
            public int CustomerId { get; set; }

            [JsonProperty("delete_quantity")]
            public int DeleteQuantity { get; set; }

            [JsonProperty("bonus_type_id")]
            public int BonusTypeId { get; set; }
        }
    }
}
