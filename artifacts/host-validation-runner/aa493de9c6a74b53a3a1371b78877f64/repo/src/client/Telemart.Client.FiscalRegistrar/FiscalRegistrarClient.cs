using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects;
using Telemart.Client.FiscalRegistrar.Entities;
using Telemart.Client.FiscalRegistrar.Requests;
using Telemart.Client.FiscalRegistrar.Responses;
using Telemart.Client.FiscalRegistrar.Responses.Base;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Common.ErrorHandling;
using Telemart.Fiscal.Client.TransferObjects;
using PrintChequeRequest = Telemart.Client.FiscalRegistrar.Requests.PrintChequeRequest;
using PrintChequeResponse = Telemart.Client.FiscalRegistrar.Responses.PrintChequeResponse;
using SessionResponse = Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects.SessionResponse;
using SessionState = Telemart.Client.FiscalRegistrar.Abstraction.TransferObjects.SessionState;

namespace Telemart.Client.FiscalRegistrar
{
    public sealed class FiscalRegistrarClient : IFiscalRegistrarClient
    {
        private const string AuthError = "Ошибка авторизации";
        private const int DefaultTimeoutSeconds = 60;
        private int delayIntervalMs;
        private Stopwatch stopwatch;
        private HttpClient httpClient;
        private FiscalRegistrarSettingsDto settings;

        public FiscalRegistrarClient()
        {
            delayIntervalMs = 200;
        }

        public FiscalRegistrarSettingsDto Settings
        {
            get => settings;
            set
            {
                SetCredentials(value.IpAddress,
                    value.Login,
                    value.Password);
                settings = value;
            }
        }

        public void SetDelayInterval(int delayIntervalMs)
        {
            this.delayIntervalMs = delayIntervalMs;
        }

        public async Task<decimal> GetCashStockAsync(int cashboxId, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "cgi/rep/Pay");

            CashStockResponse cashStockResponse = await ExecuteAsync<CashStockResponse>(request, cancellationToken);

            return cashStockResponse.CashStocks
                .Where(x => x.Id == FiscalPaymentType.CashPayment.Id)
                .Select(x => x.Sum)
                .Single();
        }

        public Task<EditTableResponse> EditPaymentTableAsync(EditPaymentTableRequest request, CancellationToken cancellationToken)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "cgi/tbl/Pay");

            string jsonBody = JsonConvert.SerializeObject(request);

            httpRequestMessage.Content = new StringContent(jsonBody);

            httpRequestMessage.Headers.Add("X-HTTP-Method-Override", "PATCH");

            return ExecuteAsync<EditTableResponse>(httpRequestMessage, cancellationToken);
        }

        private void SetCredentials(string ip, string user, string password)
        {
            stopwatch = new Stopwatch();

            httpClient = CreateHttpClient(ip, user, password);
        }

        private static HttpClient CreateHttpClient(string ip, string user, string password)
        {
            return new HttpClient(new DigestHttpClientHandler(user, password))
            {
                Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds),
                BaseAddress = new Uri($"http://{ip}/")
            };
        }

        public async Task<bool> GetStateAsync(int cashboxId, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "cgi/state");

            StateResponse stateResponse = await ExecuteAsync<StateResponse>(request, cancellationToken);

            return stateResponse.IsOk;
        }

        public async Task<Result<SellResponse>> SellAsync(SellRequest request, CancellationToken cancellationToken)
        {
            PrintChequeRequest printChequeRequest = new PrintChequeRequest(
                request.PrefixComment,
                request.PostfixComment,
                request.OrderId,
                ChequeType.Receive,
                request.Products
                    .Select(x => new PrintChequeProductItem(x.Id, x.Name, x.Price, x.Quantity))
                    .ToArray(),
                request.Payments.Select(x => new PrintChequePaymentItem(x.PaymentType.Id, x.Amount))
                    .ToArray());

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "cgi/chk");

            string jsonBody = printChequeRequest.ToString();

            httpRequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, MediaTypeNames.Application.Json);

            await ExecuteAsync<PrintChequeResponse>(httpRequestMessage, cancellationToken);

            return Result.Success( new SellResponse
            {
                Id = string.Empty,
                Printed = true
            });
        }

        public async Task<Result<SellResponse>> ReturnAsync(ReturnRequest request, CancellationToken cancellationToken)
        {
            PrintChequeRequest printChequeRequest = new PrintChequeRequest(
                request.PrefixComment,
                request.PostfixComment,
                request.OrderId,
                ChequeType.Refund,
                request.Products
                    .Select(x => new PrintChequeProductItem(x.Id, x.Name, x.Price, x.Quantity))
                    .ToArray(),
                request.Payments.Select(x => new PrintChequePaymentItem(x.PaymentType.Id, x.Amount))
                    .ToArray());

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "cgi/chk");

            string jsonBody = printChequeRequest.ToString();

            httpRequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, MediaTypeNames.Application.Json);

            await ExecuteAsync<PrintChequeResponse>(httpRequestMessage, cancellationToken);

            return Result.Success(new SellResponse
            {
                Id = string.Empty,
                Printed = true
            });
        }

        public Task PrintChequeAsync(int cashboxId, string id, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public async Task<SessionResponse> OpenSessionAsync(int cashboxId, decimal amount, CancellationToken cancellationToken)
        {
            PrintIOChequeRequest printChequeRequest = new PrintIOChequeRequest(new PrintIOChequeItem(IOChequeType.Receive, amount, FiscalPaymentType.CashPayment.Id));

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "cgi/chk");

            string jsonBody = printChequeRequest.ToJsonObject().ToString();

            httpRequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, MediaTypeNames.Application.Json);

            PrintChequeResponse response = await ExecuteAsync<PrintChequeResponse>(httpRequestMessage, cancellationToken);

            return new SessionResponse
            {
                Id = response.Id.ToString(),
                State = SessionState.Opened
            };
        }

        public Task CloseSessionAsync(int cashboxId, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "cgi/proc/printreport?0");

            return ExecuteAsync<ReportResponse>(request, cancellationToken);
        }

        public async Task<CashCollectionResponse> CashCollectionAsync(int cashboxId, decimal amount, CancellationToken cancellationToken)
        {
            PrintIOChequeRequest request = new PrintIOChequeRequest(new PrintIOChequeItem(IOChequeType.Refund, amount, FiscalPaymentType.CashPayment.Id));

            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "cgi/chk");

            string jsonBody = request.ToJsonObject().ToString();

            httpRequestMessage.Content = new StringContent(jsonBody, Encoding.UTF8, MediaTypeNames.Application.Json);

            PrintChequeResponse printChequeResponse = await ExecuteAsync<PrintChequeResponse>(httpRequestMessage, cancellationToken);

            return new CashCollectionResponse
            {
                Id = printChequeResponse.Id.ToString()
            };
        }

        public async Task<XReportResponse> XReportAsync(int cashboxId, CancellationToken cancellationToken)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "cgi/proc/printreport?10");

            await ExecuteAsync<ReportResponse>(request, cancellationToken);

            return new XReportResponse();
        }

        public Task ZReportAsync(int cashboxId, string lastZReportId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Для аппаратного РРО не поддерживается.");
        }

        public Task<bool> SendToEmailsAsync(string id, string[] emails, int cashboxId, CancellationToken cancellationToken)
        {
            throw new UnexpectedSatusException(HttpStatusCode.BadRequest, new Error
            {
                ErrorMessage = "Failed to send email",
                Details = new []{ new Error {ErrorMessage = "Отправка чека на почту не поддерживается для аппаратных РРО"}}.ToList()
            });
        }

        public Task<bool> SendByPhoneAsync(string id, string phone, int cashboxId, CancellationToken cancellationToken)
        {
            throw new UnexpectedSatusException(HttpStatusCode.BadRequest, new Error
            {
                ErrorMessage = "Failed to send sms",
                Details = new []{ new Error {ErrorMessage = "Отправка чека по sms не поддерживается для аппаратных РРО"}}.ToList()
            });
        }

        public Task<Result<QueryReceiptStatusResponse>> GetReceiptStatusAsync(int cashboxId, string receiptId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success(new QueryReceiptStatusResponse { Status = "DONE" }));
        }

        public Task<PrintRroCheckSettingsDto> GetPrintRroChecksSettingsAsync()
        {
            return Task.FromResult(new PrintRroCheckSettingsDto { Printer = true });
        }

        private async Task<TResponse> ExecuteAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
            where TResponse : FiscalRegistrarResponseBase
        {
            await WaitAsync();

            HttpResponseMessage httpResponseMessage = null;
            Exception ex = null;
            try
            {
                httpResponseMessage = await httpClient.SendAsync(request, cancellationToken);
            }
            catch (Exception e)
            {
                ex = e;
            }
            TResponse response = await HandleResponseAsync<TResponse>(ex, httpResponseMessage);

            Start();

            return response;
        }

        private static async Task<TResponse> HandleResponseAsync<TResponse>(Exception exception, HttpResponseMessage httpResponseMessage)
            where TResponse : FiscalRegistrarResponseBase
        {
            string errorMessage;

            if (exception != null)
            {
                errorMessage = exception.Message;
            }
            else
            {
                string content = await httpResponseMessage.Content.ReadAsStringAsync();

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    TResponse response = (TResponse)Activator.CreateInstance(typeof(TResponse), content);

                    if (response.IsError)
                    {
                        throw new UnexpectedSatusException(
                            HttpStatusCode.BadRequest,
                            new Error
                            {
                                ErrorCode = ErrorCode.None,
                                ErrorMessage = "Failed to execute fiscal registrar method",
                                Details = response.GetErrorMessages()
                                    .Select(x => new Error
                                    {
                                        ErrorCode = ErrorCode.None,
                                        ErrorMessage = x
                                    })
                                    .ToList()
                            });
                    }

                    return response;
                }

                if (httpResponseMessage?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    errorMessage = AuthError;
                }
                else
                {
                    errorMessage = content;
                }
            }

            throw new UnexpectedSatusException(
                httpResponseMessage?.StatusCode,
                new Error
                {
                    ErrorCode = ErrorCode.None,
                    ErrorMessage = errorMessage,
                });
        }

        private void Start()
        {
            stopwatch.Start();
        }

        private async Task WaitAsync()
        {
            if (stopwatch.IsRunning && stopwatch.ElapsedMilliseconds < delayIntervalMs)
            {
                await Task.Delay(delayIntervalMs - (int)stopwatch.ElapsedMilliseconds);
            }

            stopwatch.Reset();
        }
    }
}