using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderConfirmDto
    {
        public OrderConfirmDto(
            int id,
            DateTime deliveryTime,
            DateTime deliveryTimeTo,
            DateTime? dateComplete,
            int packageDeliveryCost,
            int packageDeliveryPaid,
            bool sendSms)
        {
            Id = id;
            DeliveryTime = deliveryTime;
            DeliveryTimeTo = deliveryTimeTo;
            ReceiveTime = dateComplete;
            PackageDeliveryCost = packageDeliveryCost;
            PackageDeliveryPaid = packageDeliveryPaid;
            SendSms = sendSms;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("delivery_time")]
        public DateTime DeliveryTime { get; set; }

        [JsonProperty("delivery_time_to")]
        public DateTime DeliveryTimeTo { get; set; }

        [JsonProperty("receive_time")]
        public DateTime? ReceiveTime { get; set; }

        [JsonProperty("package_delivery_cost")]
        public int PackageDeliveryCost { get; set; }

        [JsonProperty("package_delivery_paid")]
        public int PackageDeliveryPaid { get; set; }

        [JsonProperty("send_sms")]
        public bool SendSms { get; set; }
    }
}