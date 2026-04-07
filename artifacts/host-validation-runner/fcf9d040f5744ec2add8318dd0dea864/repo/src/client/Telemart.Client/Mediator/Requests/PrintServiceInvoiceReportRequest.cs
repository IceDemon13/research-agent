using MediatR;

namespace Telemart.Client.Mediator.Requests
{
    public class PrintServiceInvoiceReportRequest : IRequest
    {
        public PrintServiceInvoiceReportRequest(int serviceInvoiceId)
        {
            ServiceInvoiceId = serviceInvoiceId;
        }

        public int ServiceInvoiceId { get; }
    }
}
