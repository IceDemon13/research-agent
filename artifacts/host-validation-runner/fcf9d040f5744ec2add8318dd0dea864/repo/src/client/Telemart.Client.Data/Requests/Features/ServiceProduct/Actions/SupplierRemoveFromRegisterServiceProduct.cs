using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public sealed class SupplierRemoveFromRegisterServiceProduct : CallEntityActionWithBodyRequestResultBase<ServiceProductDto, SupplierRemoveFromRegisterServiceProduct.SupplierRemoveFromRegisterServiceProductDto>
    {
        public SupplierRemoveFromRegisterServiceProduct(int serviceProductId, decimal amount, int currencyId, DateTime date, string comment)
            : base(
                serviceProductId,
                new SupplierRemoveFromRegisterServiceProductDto
                {
                    Id = serviceProductId,
                    Amount = amount,
                    CurrencyId = currencyId,
                    Date = date,
                    Comment = comment
                },
                ApiResources.ServiceProducts,
                "supplier_remove")
        {
        }

        public class SupplierRemoveFromRegisterServiceProductDto
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("amount")]
            public decimal Amount { get; set; }

            [JsonProperty("currency_id")]
            public int CurrencyId { get; set; }

            [JsonProperty("date")]
            public DateTime Date { get; set; }

            [JsonProperty("comment")]
            public string Comment { get; set; }
        }
    }
}