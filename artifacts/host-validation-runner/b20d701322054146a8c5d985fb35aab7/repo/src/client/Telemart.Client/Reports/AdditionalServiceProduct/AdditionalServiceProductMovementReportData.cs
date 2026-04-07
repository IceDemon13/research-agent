using System;

namespace Telemart.Client.Reports.AdditionalServiceProduct
{
    public class AdditionalServiceProductMovementReportData
    {
        public AdditionalServiceProductMovementReportData(int additionalServiceProductId, int orderId, DateTime? orderDeliveryTimeTo, DateTime? completedOn)
        {
            Barcode = $"ADS-{additionalServiceProductId}";
            AdditionalServiceProductId = additionalServiceProductId;
            OrderId = orderId;
            OrderDeliveryTimeToStr = orderDeliveryTimeTo?.ToString("dd.MM.yy HH:mm");
            CompletedOnStr = completedOn?.ToString("dd.MM.yy HH:mm");
        }

        public string Barcode { get; set; }

        public int AdditionalServiceProductId { get; set; }

        public int OrderId { get; set; }

        public string OrderDeliveryTimeToStr { get; set; }

        public string CompletedOnStr { get; set; }
    }
}