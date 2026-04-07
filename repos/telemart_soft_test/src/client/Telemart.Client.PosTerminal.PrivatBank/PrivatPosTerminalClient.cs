using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Namotion.Reflection;
using Newtonsoft.Json;
using Serilog;
using Telemart.Client.PosTerminal.Ingenico;
using Telemart.Client.PosTerminal.PrivatBank.Entity;
using Telemart.Client.PosTerminal.PrivatBank.Requests;
using Telemart.Client.PosTerminal.PrivatBank.Responses;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.PosTerminal.PrivatBank
{
    public class PrivatPosTerminalClient : IPrivatBankPosTerminalClient, IDisposable
    {
        private Process process;
        private string _ip;
        private int _port;
        private bool _disposed;
        private ILogger<PrivatPosTerminalClient> _logger;

        public PrivatPosTerminalClient(ILogger<PrivatPosTerminalClient> logger)
        {
            _logger = logger;
        }

        public PosSettingsDto Settings { get; set; }

        public void ReConnect()
        {
            Connect(_ip, _port);
        }

        public void Connect(string ip, int port)
        {
            if (string.IsNullOrWhiteSpace(ip))
            {
                throw new ArgumentException("ip is empty", nameof(ip));
            }

            _ip = ip;
            _port = port;

            Kill();

            ProcessStartInfo processStartInfo = new ProcessStartInfo(@"Tools\genericDriverJsonETH.exe", $"-ip {ip}");

            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;

            process = Process.Start(processStartInfo);
        }

        public async Task<Result<TerminalPurchaseResult>> PurchaseAsync(
            decimal amount,
            decimal discount,
            string merchantId,
            IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            Response<PurchaseResponseItem> response =
                await ExecuteAsync(new PurchaseRequest(amount, discount, merchantId));

            return response.IsValid()
                ? Result<TerminalPurchaseResult>.Success(
                    new TerminalPurchaseResult(
                        response.Parameter.Rrn,
                        uint.Parse(response.Parameter.InvoiceNumber),
                        response.Parameter.TerminalId,
                        response.Parameter.Merchant,
                        response.Parameter.ApprovalCode,
                        response.Parameter.Pan,
                        response.Parameter.IssuerName))
                : Result<TerminalPurchaseResult>.Error(
                    "Error terminals transaction",
                    response.GetErrorMessages().ToArray());
        }

        public async Task<Result<ReturnRefundResult>> ReturnRefundAsync(
            decimal amount,
            bool fullAmountTransaction,
            string merchantId,
            string rn,
            string rrn,
            uint? invoiceNum,
            decimal? discount,
            DateTime? dateTimePuchase,
            IProgress<string> progress,
            CancellationToken cancellationToken)
        {
            if (dateTimePuchase.HasValue && dateTimePuchase.Value.Date == DateTime.Now.Date)
            {
                return await CancelAsync(invoiceNum, progress, cancellationToken);
            }

            return await RefundAsync(amount, discount, rrn, merchantId, progress, cancellationToken);
        }

        private async Task<Result<ReturnRefundResult>> CancelAsync(uint? invoiceNum, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Response<PurchaseResponseItem> response = await ExecuteAsync(new CancelRequest(invoiceNum.ToString()));

            return response.IsValid()
                ? Result.Success(new ReturnRefundResult(
                        response.Parameter.Rrn,
                        uint.Parse(response.Parameter.InvoiceNumber),
                        response.Parameter.TerminalId,
                        response.Parameter.Merchant,
                        response.Parameter.ApprovalCode,
                        response.Parameter.Pan,
                        response.Parameter.IssuerName))
                : Result<ReturnRefundResult>.Error("Failed to cancel terminal payment", response.GetErrorMessages().ToArray());
        }

        private async Task<Result<ReturnRefundResult>> RefundAsync(decimal amount, decimal? discount, string rrn, string merchant, IProgress<string> progress, CancellationToken cancellationToken)
        {
            Response<PurchaseResponseItem> response = await ExecuteAsync(new RefundRequest(amount, discount ?? 0, merchant, rrn));

            return response.IsValid()
                ? Result.Success(new ReturnRefundResult(
                    response.Parameter.Rrn,
                    uint.Parse(response.Parameter.InvoiceNumber),
                    response.Parameter.TerminalId,
                    response.Parameter.Merchant,
                    response.Parameter.ApprovalCode,
                    response.Parameter.Pan,
                    response.Parameter.IssuerName))
                : Result<ReturnRefundResult>.Error("Failed to refund terminal payment", response.GetErrorMessages().ToArray());
        }

        private async Task<Response<TResponse>> ExecuteAsync<TRequest, TResponse>(
            RequestBase<TRequest, TResponse> request) where TResponse : ResponseItemBase
        {
            if (!IsConnected())
            {
                ReConnect();

                await Task.Delay(TimeSpan.FromMinutes(1));
            }

            ClientWebSocket ws = new ClientWebSocket();

            Uri url = new Uri("ws://localhost:3000/echo");

            await ws.ConnectAsync(url, CancellationToken.None);

            if (ws.State != WebSocketState.Open)
            {
                return new Response<TResponse>("Failed connect to POS terminal");
            }

            string requestJson = JsonConvert.SerializeObject(request);
            string responseJson = null;

            try
            {
                ArraySegment<byte> requestBytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(requestJson));

                await ws.SendAsync(requestBytes, WebSocketMessageType.Text, true, CancellationToken.None);

                using (MemoryStream ms = new MemoryStream())
                {
                    ArraySegment<byte> responseBytes = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(responseBytes, CancellationToken.None);

                        if (responseBytes.Array != null)
                        {
                            await ms.WriteAsync(responseBytes.Array, responseBytes.Offset, result.Count);
                        }
                    } while (!result.EndOfMessage);

                    ms.Seek(0, SeekOrigin.Begin);

                    responseJson = Encoding.UTF8.GetString(ms.ToArray().Where(x => x > 0x00).ToArray());
                }
            }
            finally
            {
                _logger.LogInformation($"Request: {requestJson}. Response: {responseJson}");

                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Normal Closure", CancellationToken.None);
            }

            return JsonConvert.DeserializeObject<Response<TResponse>>(responseJson);
        }

        public bool IsConnected()
        {
            return process?.HasExited == false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Kill();

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }

        public void Kill()
        {
            foreach (Process processToKill in Process.GetProcessesByName("genericDriverJsonETH")
                         .Where(x => !x.HasExited))
            {
                processToKill.Kill();
            }

            process?.Dispose();
            process = null;
        }

        ~PrivatPosTerminalClient()
        {
            Dispose();
        }
    }
}