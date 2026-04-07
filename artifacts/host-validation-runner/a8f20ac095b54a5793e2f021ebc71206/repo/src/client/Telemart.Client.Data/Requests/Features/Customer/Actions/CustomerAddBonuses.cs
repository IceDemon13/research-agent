using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Data.Requests.Features.Customer.Actions
{
    public class CustomerAddBonuses : CallEntityActionWithBodyRequestResultBase<CustomerDto, CustomerAddBonuses.AddBonusDto>
    {
        public CustomerAddBonuses(int customerId, int quantity, int bonusTypeId, DateTime? expireDate)
            : base(customerId, new AddBonusDto(customerId, quantity, bonusTypeId, expireDate), ApiResources.Customers, "add_bonuses")
        {
        }

        public class AddBonusDto
        {
            public AddBonusDto(int customerId, int quantity, int bonusTypeId, DateTime? expireDate)
            {
                CustomerId = customerId;
                Quantity = quantity;
                BonusTypeId = bonusTypeId;
                ExpireDate = expireDate;
            }

            [JsonProperty("customer_id")]
            public int CustomerId { get; set; }

            [JsonProperty("quantity")]
            public int Quantity { get; set; }

            [JsonProperty("bonus_type_id")]
            public int BonusTypeId { get; set; }

            [JsonProperty("expire_date")]
            public DateTime? ExpireDate { get; set; }
        }
    }
}