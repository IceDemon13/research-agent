using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Customer;

namespace Telemart.Client.Data.Requests.Features.Customer.Actions
{
    public sealed class CustomerTransferBonuses : CallEntityActionWithBodyRequestResultBase<CustomerDto, CustomerTransferBonuses.TransferBonusDto>
    {
        public CustomerTransferBonuses(int senderCustomerId, int recepientCustomerId, int quantity, int bonusTypeId, DateTime? dateTime)
            : base(senderCustomerId, new TransferBonusDto(senderCustomerId, recepientCustomerId, quantity, bonusTypeId, dateTime), ApiResources.Customers, "transfer_bonuses")
        {
        }

        public class TransferBonusDto
        {
            public TransferBonusDto(int senderCustomerId, int recepientCustomerId, int quantity, int bonusTypeId, DateTime? expireDate)
            {
                Quantity = quantity;
                BonusTypeId = bonusTypeId;
                SenderCustomerId = senderCustomerId;
                RecepientCustomerId = recepientCustomerId;
                BonusTypeId = bonusTypeId;
                ExpireDate = expireDate;
            }

            [JsonProperty("sender_customer_id")]
            public int SenderCustomerId { get; set; }

            [JsonProperty("recepient_customer_id")]
            public int RecepientCustomerId { get; set; }

            [JsonProperty("quantity")]
            public int Quantity { get; set; }

            [JsonProperty("bonus_type_id")]
            public int BonusTypeId { get; set; }

            [JsonProperty("expire_date")]
            public DateTime? ExpireDate { get; set; }
        }
    }
}