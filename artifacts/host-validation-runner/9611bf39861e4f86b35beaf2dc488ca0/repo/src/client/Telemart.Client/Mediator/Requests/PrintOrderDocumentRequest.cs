using MediatR;
using Telemart.Client.Business.Order;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Mediator.Requests
{
    public sealed class PrintOrderDocumentRequest : IRequest
    {
        public PrintOrderDocumentRequest(int orderId, int orderDocumentTypeId, object parameter = null, bool todayAsIssueDate = false, bool preview = true, short copies = 1)
        {
            OrderId = orderId;
            OrderDocumentTypeId = orderDocumentTypeId;
            Parameter = parameter;
            TodayAsIssueDate = todayAsIssueDate;
            Preview = preview;
            Copies = copies;
        }

        public int OrderDocumentTypeId { get; }

        public int OrderId { get; }

        public object Parameter { get; }

        public bool TodayAsIssueDate { get; }

        public bool Preview { get; }

        public short Copies { get; }
    }
}