using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Refit;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data;
using Telemart.Client.Data.Authentication;
using Telemart.Client.Data.Options;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects;
using Telemart.Client.FiscalRegistrar.Programmical;
using Telemart.Client.FiscalRegistrar.Programmical.TransferObjects;
using Telemart.Client.Reports;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Client.ViewModels;
using Telemart.Client.ViewModels.Common;
using Telemart.Common.ErrorHandling;
using Telemart.Fiscal.Client.TransferObjects;
using Application = System.Windows.Application;
using PrintChequeRequest = Telemart.Client.FiscalRegistrar.Programmical.TransferObjects.PrintChequeRequest;
using PrintReceiptType = Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects.PrintReceiptType;
using PrintReportRequest = Telemart.Client.FiscalRegistrar.Programmical.TransferObjects.PrintReportRequest;
using PrintResponse = Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects.PrintResponse;
using SessionResponse = Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects.SessionResponse;
using SessionState = Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects.SessionState;

namespace Telemart.Client.FiscalRegistrar
{
    public class ProgrammicalFiscalRegistrarClient : IFiscalRegistrarClient
    {
        private readonly int width;
        private readonly IFiscalRegistrarApi api;
        private readonly IResponseHandler responseHandler;
        private readonly IAuthenticationManager authenticationManager;
        private readonly IPrintingSettingsStore printingSettingsStore;
        private readonly IMediator mediator;
        private readonly ILogger<ProgrammicalFiscalRegistrarClient> logger;
        private readonly PrintRroOptions printRroOptions;

        public ProgrammicalFiscalRegistrarClient(
            IResponseHandler responseHandler,
            IAuthenticationManager authenticationManager,
            IPrintingSettingsStore printingSettingsStore,
            IMediator mediator,
            FiscalServiceOptions serviceOptions,
            FiscalOptions options,
            PrintRroOptions printRroOptions,
            ILogger<ProgrammicalFiscalRegistrarClient> logger)
        {
            this.responseHandler = responseHandler;
            this.authenticationManager = authenticationManager;
            this.printingSettingsStore = printingSettingsStore;
            this.mediator = mediator;
            this.logger = logger;

            width = options.TapeWidth;
            this.printRroOptions = printRroOptions;

            string baseAddress = serviceOptions.BaseAddress;

            api = RestService.For<IFiscalRegistrarApi>(baseAddress);
        }

        public FiscalRegistrarSettingsDto Settings { get; set; }

        public async Task<decimal> GetCashStockAsync(int cashboxId, CancellationToken cancellationToken)
        {
            Result<XReportResponse> response = await ExecuteAsync<Result<XReportResponse>>((t, ct) => api.XReportAsync(cashboxId, t, ct), cancellationToken);

            return response.Data.Balance;
        }

        public async Task<bool> GetStateAsync(int cashboxId, CancellationToken cancellationToken)
        {
            string response = await ExecuteAsync<string>((t, ct) => api.PingAsync(cashboxId, t, ct), cancellationToken);

            return !string.IsNullOrEmpty(response);
        }

        public async Task<Result<SellResponse>> SellAsync(SellRequest request, CancellationToken cancellationToken)
        {
            Result<SellResponse> sellResponse = await ExecuteAsync<Result<SellResponse>>((t, ct) => api.SellAsync(request.CashboxId, request, t, ct), cancellationToken);

            sellResponse.Data.Printed = false;

            return Result.Success(sellResponse.Data);
        }

        public async Task<Result<SellResponse>> ReturnAsync(ReturnRequest request, CancellationToken cancellationToken)
        {
            Result<SellResponse> sellResponse = await ExecuteAsync<Result<SellResponse>>((t, ct) => api.ReturnAsync(request.CashboxId, request, t, ct), cancellationToken);

            sellResponse.Data.Printed = false;

            return Result.Success(sellResponse.Data);
        }

        public async Task<SessionResponse> OpenSessionAsync(int cashboxId, decimal amount, CancellationToken cancellationToken)
        {
            Result<SessionResponse> result = await ExecuteAsync<Result<SessionResponse>>((t, ct) => api.OpenSessionAsync(cashboxId, t, ct), cancellationToken);

            AsyncRetryPolicy<Result<SessionResponse>> policy = Policy.HandleResult<Result<SessionResponse>>(x => x.Data.State == SessionState.Created)
                .WaitAndRetryAsync(10, _ => TimeSpan.FromSeconds(1));

            Result<SessionResponse> response = await policy.ExecuteAsync(_ => ExecuteAsync<Result<SessionResponse>>((t, ct) => api.GetSessionAsync(cashboxId, result.Data.Id, t, ct), cancellationToken), cancellationToken);

            if (response.Data.State != SessionState.Opened)
            {
                throw new UnexpectedSatusException(null, new Error()
                {
                    ErrorMessage = "Не удалось открыть смену в РРО"
                });
            }

            return result.Data;
        }

        public async Task CloseSessionAsync(int cashboxId, CancellationToken cancellationToken)
        {
            Result<CloseSessionResponse> сloseSessionResponse = await ExecuteAsync<Result<CloseSessionResponse>>((t, ct) => api.CloseSessionAsync(cashboxId, t, ct), cancellationToken);

            try
            {
                await PrintReportAsync(cashboxId, сloseSessionResponse.Data.ZReportId, "Z отчет", cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to print Z report. Ref: {Ref}", сloseSessionResponse.Data.ZReportId);
            }
        }

        public async Task<CashCollectionResponse> CashCollectionAsync(int cashboxId, decimal amount, CancellationToken cancellationToken)
        {
            CashCollectionRequest request = new CashCollectionRequest
            {
                Id = cashboxId,
                Amount = amount
            };

            Result<CashCollectionResponse> cashCollectionResult = await ExecuteAsync<Result<CashCollectionResponse>>((t, ct) => api.CashCollectionAsync(cashboxId, request, t, ct), cancellationToken);

            return cashCollectionResult.Data;
        }

        public async Task<XReportResponse> XReportAsync(int cashboxId, CancellationToken cancellationToken)
        {
            AsyncRetryPolicy policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(4));

            Result<XReportResponse> xReportResponse = await policy.ExecuteAsync(
                _ => ExecuteAsync<Result<XReportResponse>>(
                    (t, ct) => api.XReportAsync(cashboxId, t, ct),
                    cancellationToken),
                cancellationToken);

            await PrintReportAsync(cashboxId, xReportResponse.Data.Id, "X отчет", cancellationToken);

            return xReportResponse.Data;
        }

        public async Task ZReportAsync(int cashboxId, string lastZReportId, CancellationToken cancellationToken)
        {
            try
            {
                await PrintReportAsync(cashboxId, lastZReportId, "Z отчет", cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to print Z report. Ref: {Ref}", lastZReportId);
            }
        }

        public async Task<bool> SendToEmailsAsync(string id, string[] emails, int cashboxId, CancellationToken cancellationToken)
        {
            SendToEmailsRequest request = new SendToEmailsRequest
            {
                CashboxId = cashboxId,
                ReceiptId = id,
                Emails = emails
            };

            AsyncRetryPolicy policy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(printRroOptions.Retries, (_) => TimeSpan.FromSeconds(printRroOptions.DelaySeconds));

            Result<string> result = await policy.ExecuteAsync(() => ExecuteAsync<Result<string>>((t, ct) => api.SendToEmailsAsync(request, t, ct), cancellationToken));

            return result?.IsSuccess == true;
        }

        public async Task<bool> SendByPhoneAsync(string id, string phone, int cashboxId, CancellationToken cancellationToken)
        {
            SendByPhoneRequest request = new SendByPhoneRequest
            {
                CashboxId = cashboxId,
                ReceiptId = id,
                Phone = phone
            };

            AsyncRetryPolicy policy = Policy.Handle<Exception>().WaitAndRetryAsync(printRroOptions.Retries, (_) => TimeSpan.FromSeconds(printRroOptions.DelaySeconds));

            Result<string> result = await policy.ExecuteAsync(() => ExecuteAsync<Result<string>>((t, ct) => api.SendByPhonesAsync(request, t, ct), cancellationToken));

            return result?.IsSuccess == true;
        }

        public async Task PrintChequeAsync(int cashboxId, string id, CancellationToken cancellationToken)
        {
            AsyncRetryPolicy policy = Policy.Handle<Exception>().WaitAndRetryAsync(printRroOptions.Retries, _ => TimeSpan.FromSeconds(printRroOptions.DelaySeconds));

            await policy.ExecuteAsync(ct => RroPrintChequeAsync(cashboxId, id, ct), cancellationToken);
        }

        public async Task<Result<QueryReceiptStatusResponse>> GetReceiptStatusAsync(int cashboxId, string receiptId, CancellationToken cancellationToken)
        {
            AsyncRetryPolicy policy = Policy.Handle<Exception>().WaitAndRetryAsync(1, (_) => TimeSpan.FromMilliseconds(50));

            Result<QueryReceiptStatusResponse> result = await policy.ExecuteAsync(() => ExecuteAsync<Result<QueryReceiptStatusResponse>>((t, ct) => api.GetReceiptStatusAsync(cashboxId, receiptId, t, ct), cancellationToken));

            return Result.Success(result.Data);
        }

        public async Task<PrintRroCheckSettingsDto> GetPrintRroChecksSettingsAsync()
        {
            try
            {
                Result<PrintRroCheckSettingsDto> result = await ExecuteAsync<Result<PrintRroCheckSettingsDto>>((t, ct) => api.GetPrintRroChecksSettings(), CancellationToken.None);

                return result?.Data;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get settings print rro checks");

                return null;
            }
        }

        private async Task PrintChequeAsync(string text, Image qrImage, CancellationToken cancellationToken)
        {
            PrintingSettingsInfo printingSettings = await printingSettingsStore.LoadAsync();

            TextReportData data = new TextReportData(text, width, qrImage);

            TextReport report = new TextReport
            {
                DataSource = new[] { data }
            };

            Mediator.Requests.PrintReportRequest request = new(report, false, printingSettings.Cheque.Name, printingSettings.Cheque.PaperSource);

            await Application.Current.Dispatcher.InvokeAsync(async () => await mediator.Send(request, cancellationToken));
        }

        private async Task<T> ExecuteAsync<T>(Func<string, CancellationToken, Task<IApiResponse<HttpResponseMessage>>> func, CancellationToken cancellationToken)
        {
            IApiResponse<HttpResponseMessage> response = null;

            await authenticationManager.RefreshTokensAsync();

            response = await func(authenticationManager.AccessToken, cancellationToken);

            if (response.Error != null)
            {
                string requestBody = string.Empty;

                try
                {
                    requestBody = await response.RequestMessage?.Content?.ReadAsStringAsync(cancellationToken);
                }
                catch
                {
                    // ignored
                }

                throw await responseHandler.HandleResponseAsync(
                    requestBody,
                    response.RequestMessage.Method,
                    response.RequestMessage.RequestUri?.ToString(),
                    Encoding.UTF8.GetBytes(response.Error.Content ?? string.Empty),
                    response.StatusCode,
                    HttpStatusCode.OK);
            }

            byte[] responseBytes = await response.Content.Content.ReadAsByteArrayAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                string content = Encoding.UTF8.GetString(responseBytes);

                if (typeof(T) == typeof(string))
                {
                    return (T)(object)content;
                }

                T result = JsonSerializer.Deserialize<T>(content);

                return result;
            }

            Exception ex = await responseHandler.HandleResponseAsync(
                null,
                response.RequestMessage.Method,
                response.RequestMessage.RequestUri?.ToString(),
                responseBytes,
                response.StatusCode,
                HttpStatusCode.OK);

            if (ex != null)
            {
                throw ex;
            }

            return default;
        }

        private async Task RroPrintChequeAsync(int cashboxId, string id, CancellationToken cancellationToken)
        {
            Result<PrintResponse> qrCodeResponse = await ExecuteAsync<Result<PrintResponse>>((t, ct) => api.GetQrCodeAsync(id, t, ct), cancellationToken);

            PrintChequeRequest printChequeRequest = new PrintChequeRequest
            {
                Id = cashboxId,
                ReceiptId = id,
                Type = PrintReceiptType.Text,
                Width = width.ToInches()
            };

            Result<PrintResponse> printResponse = await ExecuteAsync<Result<PrintResponse>>((t, ct) => api.PrintSellAsync(cashboxId, printChequeRequest, t, ct), cancellationToken);

            string text = Encoding.UTF8.GetString(Convert.FromBase64String(printResponse.Data.Body));

            byte[] qrbytes = Convert.FromBase64String(qrCodeResponse.Data.Body);

            await using MemoryStream ms = new(qrbytes);

            Image image = Image.FromStream(ms);

            await PrintChequeAsync(text, image, cancellationToken);
        }

        private async Task PrintReportAsync(int cashboxId, string reportId, string caption, CancellationToken cancellationToken)
        {
            PrintingSettingsInfo printingSettings = await printingSettingsStore.LoadAsync();

            PrintReportRequest printReportRequest = new PrintReportRequest
            {
                Id = cashboxId,
                ReportId = reportId,
                Type = PrintReportType.Text,
                Width = width.ToInches()
            };

            Result<PrintResponse> printResponse = await ExecuteAsync<Result<PrintResponse>>((t, ct) => api.PrintReportAsync(cashboxId, printReportRequest, t, ct), cancellationToken);

            ISupportServices supportServices = ((MainWindowViewModel)Application.Current.MainWindow.DataContext).WorkspaceViewModel;

            IDocumentManagerService documentManagerService = supportServices.ServiceContainer.GetService<IDocumentManagerService>("DefaultPositionDialogDocumentManagerService", ServiceSearchMode.PreferParents);

            ShowTextParameter parameter = new ShowTextParameter(
                caption,
                Encoding.UTF8.GetString(Convert.FromBase64String(printResponse.Data.Body)),
                true,
                width,
                printingSettings.Cheque);

            documentManagerService.ShowView<ShowTextViewModel>(parameter, supportServices);
        }
    }
}