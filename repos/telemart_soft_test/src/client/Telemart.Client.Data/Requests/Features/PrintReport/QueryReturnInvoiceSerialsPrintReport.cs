using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.ReturnInvoice;

namespace Telemart.Client.Data.Requests.Features.PrintReport
{
    public sealed class QueryReturnInvoiceSerialsPrintReport : QueryRequestBase<ReturnInvoiceReportDataDto>
    {
        public QueryReturnInvoiceSerialsPrintReport(int invoiceId, int? returnInvoiceId = null)
            : base(ApiResources.PrintReport, $"invoice/{invoiceId}/serials/{ApiResources.ReturnInvoices}/{returnInvoiceId}")
        {
        }
    }
}