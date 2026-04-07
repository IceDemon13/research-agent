using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ReturnInvoice
{
    public sealed class ReturnInvoiceChangeAddressDto
    {
        public ReturnInvoiceChangeAddressDto(
            DateTime returnDate,
            int warehouseId,
            int carryId,
            int? ttnPayerTypeId,
            int? receiverCityId,
            DeliveryDataDto deliveryDataDto)
        {
            ReturnDate = returnDate;
            WarehouseId = warehouseId;
            CarryId = carryId;
            TtnPayerTypeId = ttnPayerTypeId;
            DeliveryData = deliveryDataDto;
            ReceiverCityId = receiverCityId;
        }

        [JsonProperty("return_date")]
        public DateTime ReturnDate { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("ttn_payer_type_id")]
        public int? TtnPayerTypeId { get; set; }

        [JsonProperty("receiver_city_id")]
        public int? ReceiverCityId { get; set; }

        [JsonProperty("delivery_data")]
        public DeliveryDataDto DeliveryData { get; set; }
    }
}