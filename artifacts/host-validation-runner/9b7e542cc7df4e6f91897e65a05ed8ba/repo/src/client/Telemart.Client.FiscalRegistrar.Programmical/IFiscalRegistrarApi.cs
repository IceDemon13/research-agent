using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Refit;
using Telemart.Client.FiscalRegistrar.Programmical.TransferObjects;
using Telemart.Fiscal.Client.TransferObjects;
using PrintChequeRequest = Telemart.Client.FiscalRegistrar.Programmical.TransferObjects.PrintChequeRequest;

namespace Telemart.Client.FiscalRegistrar.Programmical
{
    public interface IFiscalRegistrarApi
    {
        [Post("/api/v1/session/{cashboxId}/actions/open")]
        Task<IApiResponse<HttpResponseMessage>> OpenSessionAsync(int cashboxId, [Authorize] string token, CancellationToken cancellationToken);

        [Get("/api/v1/session/{cashboxId}/session/{sessionId}")]
        Task<IApiResponse<HttpResponseMessage>> GetSessionAsync(int cashboxId, string sessionId, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/{cashboxId}/actions/close")]
        Task<IApiResponse<HttpResponseMessage>> CloseSessionAsync(int cashboxId, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/{cashboxId}/actions/cash_collection")]
        Task<IApiResponse<HttpResponseMessage>> CashCollectionAsync(int cashboxId, [Body] CashCollectionRequest request, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/{cashboxId}/actions/x_report")]
        Task<IApiResponse<HttpResponseMessage>> XReportAsync(int cashboxId, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/{cashboxId}/actions/sell")]
        Task<IApiResponse<HttpResponseMessage>> SellAsync(int cashboxId, [Body] SellRequest request, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/{cashboxId}/actions/return")]
        Task<IApiResponse<HttpResponseMessage>> ReturnAsync(int cashboxId, [Body] ReturnRequest request, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/{cashboxId}/actions/print_sell")]
        Task<IApiResponse<HttpResponseMessage>> PrintSellAsync(int cashboxId, [Body] PrintChequeRequest request, [Authorize] string token, CancellationToken cancellationToken);

        [Get("/api/v1/session/receipt_qr/{receiptId}")]
        Task<IApiResponse<HttpResponseMessage>> GetQrCodeAsync(string receiptId, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/{cashboxId}/actions/print_report")]
        Task<IApiResponse<HttpResponseMessage>> PrintReportAsync(int cashboxId, [Body] PrintReportRequest request, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/{cashboxId}/actions/ping")]
        Task<IApiResponse<HttpResponseMessage>> PingAsync(int cashboxId, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/actions/send_emails")]
        Task<IApiResponse<HttpResponseMessage>> SendToEmailsAsync([Body] SendToEmailsRequest request, [Authorize] string token, CancellationToken cancellationToken);

        [Post("/api/v1/session/actions/send_sms")]
        Task<IApiResponse<HttpResponseMessage>> SendByPhonesAsync([Body] SendByPhoneRequest request, [Authorize] string token, CancellationToken cancellationToken);

        [Get("/api/v1/session/{cashboxId}/receipt_status/{receiptId}")]
        Task<IApiResponse<HttpResponseMessage>> GetReceiptStatusAsync(int cashboxId, string receiptId, [Authorize] string token, CancellationToken cancellationToken);

        [Get("/api/v1/settings/print_rro_checks")]
        Task<IApiResponse<HttpResponseMessage>> GetPrintRroChecksSettings();
    }
}