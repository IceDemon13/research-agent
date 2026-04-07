using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierCurrency
{
    public sealed class SupplierCurrencyRatesCreateDto
    {
        public SupplierCurrencyRatesCreateDto(SupplierCurrencyRateCreateDto[] supplierCurrencyRateCreateDtos)
        {
            SupplierCurrencyRateCreateDtos = supplierCurrencyRateCreateDtos;
        }

        [JsonProperty("supplier_currency_rates")]
        public SupplierCurrencyRateCreateDto[] SupplierCurrencyRateCreateDtos { get; set; }
    }
}