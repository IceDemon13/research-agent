using System;

namespace Telemart.Client.Reports.AdditionalServiceProduct
{
    public class AdditionalServiceProductMovementReportData
    {
        public AdditionalServiceProductMovementReportData(int additionalServiceProductId, int orderId, DateTime? orderDeliveryTymeTo, DateTime? completedOn)
        {
            Barcode = $"ADS-{additionalServiceProductId}";
            AdditionalServiceProductId = additionalServiceProductId;
            OrderId = orderId;
            OrderDeliveryTymeTo = orderDeliveryTymeTo;
            CompletedOn = completedOn;
        }

        public string Barcode { get; set; }

        public int AdditionalServiceProductId { get; set; }

        public int OrderId { get; set; }

        public DateTime? OrderDeliveryTymeTo { get; set; }

        public DateTime? CompletedOn { get; set; }
    }
}