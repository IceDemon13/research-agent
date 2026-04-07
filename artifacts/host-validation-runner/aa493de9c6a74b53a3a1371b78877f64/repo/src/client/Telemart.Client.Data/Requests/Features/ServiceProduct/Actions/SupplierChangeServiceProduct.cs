using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.ServiceProduct;

namespace Telemart.Client.Data.Requests.Features.ServiceProduct.Actions
{
    public sealed class SupplierChangeServiceProduct : CallEntityActionWithBodyRequestResultBase<ServiceProductDto, SupplierChangeServiceProduct.SupplierChangeServiceProductDto>
    {
        public SupplierChangeServiceProduct(
            int serviceProductId,
            int changeOnProductId,
            string changeOnProductSerial,
            decimal amount,
            int currencyId,
            DateTime date,
            string comment)
            : base(
                serviceProductId,
                new SupplierChangeServiceProductDto
                {
                    Id = serviceProductId,
                    ChangeOnProductId = changeOnProductId,
                    ChangeOnProductSerial = changeOnProductSerial,
                    Amount = amount,
                    CurrencyId = currencyId,
                    Date = date,
                    Comment = comment
                },
                ApiResources.ServiceProducts,
                "supplier_change")
        {
        }

        public class SupplierChangeServiceProductDto
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("change_on_product_id")]
            public int ChangeOnProductId { get; set; }

            [JsonProperty("change_on_product_sn")]
            public string ChangeOnProductSerial { get; set; }

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