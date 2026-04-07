using Telemart.Client.Data.Requests.Features.Report.TransferObjects;

namespace Telemart.Client.Common.Messages
{
    public sealed class ReportMessage : EntityMessage<ReportDto>
    {
        public ReportMessage(ReportDto entity, MessageType messageType)
            : base(entity, messageType)
        {
        }
    }
}