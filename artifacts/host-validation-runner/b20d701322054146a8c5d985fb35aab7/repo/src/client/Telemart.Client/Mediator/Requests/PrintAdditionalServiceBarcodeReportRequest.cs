using System;
using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public sealed class PrintAdditionalServiceBarcodeReportRequest : IRequest
    {
        public PrintAdditionalServiceBarcodeReportRequest(int additionalServiceProductId, int orderId, DateTime? orderDeliveryTimeTo, DateTime? completedOn)
        {
            AdditionalServiceProductId = additionalServiceProductId;
            OrderId = orderId;
            OrderDeliveryTimeTo = orderDeliveryTimeTo;
            CompletedOn = completedOn;
        }

        public int AdditionalServiceProductId { get; }

        public int OrderId { get; }

        public DateTime? OrderDeliveryTimeTo { get; }

        public DateTime? CompletedOn { get; }
    }
}