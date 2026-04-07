using System.Threading;
using System.Threading.Tasks;
using Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Common.ErrorHandling;
using Telemart.Fiscal.Client.TransferObjects;
using SessionResponse = Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects.SessionResponse;

namespace Telemart.Client.FiscalRegistrar.Abstraction
{
    public interface IFiscalRegistrarClient
    {
        FiscalRegistrarSettingsDto Settings { get; set; }

        Task<decimal> GetCashStockAsync(int cashboxId, CancellationToken cancellationToken);

        Task<bool> GetStateAsync(int cashboxId, CancellationToken cancellationToken);

        Task<Result<SellResponse>> SellAsync(SellRequest request, CancellationToken cancellationToken);

        Task<Result<SellResponse>> ReturnAsync(ReturnRequest request, CancellationToken cancellationToken);

        Task PrintChequeAsync(int cashboxId, string id, CancellationToken cancellationToken);

        Task<SessionResponse> OpenSessionAsync(int cashboxId, decimal amount, CancellationToken cancellationToken);

        Task CloseSessionAsync(int cashboxId, CancellationToken cancellationToken);

        Task<CashCollectionResponse> CashCollectionAsync(int cashboxId, decimal amount, CancellationToken cancellationToken);

        Task<XReportResponse> XReportAsync(int cashboxId, CancellationToken cancellationToken);

        Task ZReportAsync(int cashboxId, string lastZReportId, CancellationToken cancellationToken);

        Task<bool> SendToEmailsAsync(string id, string[] emails, int cashboxId, CancellationToken cancellationToken);

        Task<bool> SendByPhoneAsync(string id, string phone, int cashboxId, CancellationToken cancellationToken);

        Task<Result<QueryReceiptStatusResponse>> GetReceiptStatusAsync(int cashboxId, string receiptId, CancellationToken cancellationToken);

        Task<PrintRroCheckSettingsDto> GetPrintRroChecksSettingsAsync();
    }
}