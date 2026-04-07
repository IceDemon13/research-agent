using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using NetEcr;
using Newtonsoft.Json;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.PosTerminal.Ingenico
{
    public class IngenicoPosTerminalClient : IPosTerminalClient
    {
        protected readonly ILogger<IngenicoPosTerminalClient> _logger;
        protected string _ip;
        protected string _port;

        private const string ReturnMoneyConnectionError = "Ошибка связи с терминалом при выполнении возврата денег";
        private const string UnknownError = "Неизвестная ошибка";

        public IngenicoPosTerminalClient(ILogger<IngenicoPosTerminalClient> logger)
        {
            _logger = logger;
        }

        public PosSettingsDto Settings { get; set; }

        public void Connect(string ip, int port)
        {
            _ip = ip;
            _port = port.ToString();
        }

        public bool IsConnected()
        {
            IBPOS1Lib terminal = new BPOS1LibClass();
            terminal.CommOpenTCP(_ip, _port);

            bool isConnected = terminal.LastResult == 0;

            terminal.CommClose();

            return isConnected;
        }

        public void Kill()
        {
        }

        public async Task<Result<TerminalPurchaseResult>> PurchaseAsync(decimal amount, decimal discount, string merchantId, IProgress<string> progress, CancellationToken cancellationToken)
        {
            List<string> errors = new List<string>();

            TerminalPurchaseResult terminalResult = default;

            bool isConfirmSend = false;

            if (!byte.TryParse(merchantId, out byte merchant))
            {
                return Result<TerminalPurchaseResult>.Error("Error terminals transaction", "Не валидный мерчант");
            }

            IBPOS1Lib terminal = new BPOS1LibClass();

            terminal.CommOpenTCP(_ip, _port);

            _logger.LogInformation("Connect: {Context}", JsonConvert.SerializeObject(terminal));

            if (terminal.LastResult != 0)
            {
                progress.Report("Ошибка связи с терминалом");
                errors.Add("Ошибка связи с терминалом");
                _logger.LogError("Ошибка связи с терминалом при выполнении оплаты");
            }
            else
            {
                terminal.SetErrorLang(2);

                terminal.Purchase((uint) (amount * 100), (uint) (discount * 100), merchant);

                _logger.LogInformation("Purchase: {Context}", JsonConvert.SerializeObject(terminal));

                progress.Report("Ожидание карты");

                _logger.LogInformation("RRN {Rrn}, InvoiceNum {InvoiceNum}, ScenarioData = {ScenarioData}", terminal.RRN, terminal.InvoiceNum, terminal.ScenarioData);

                do
                {
                    if (terminal.LastResult == 2)
                    {
                        await Task.Delay(500, cancellationToken);
                    }

                    if (terminal.LastResult == 0)
                    {
                        if (isConfirmSend == false)
                        {
                            isConfirmSend = true;

                            terminal.Confirm();

                            terminalResult = new TerminalPurchaseResult(
                                terminal.RRN,
                                terminal.InvoiceNum,
                                terminal.TerminalID,
                                terminal.MerchantID,
                                terminal.AuthCode,
                                terminal.PAN,
                                terminal.IssuerName);

                            string scenarioData = terminal.ScenarioData;

                            _logger.LogInformation("Подтверждение операции (Confirm) отправлено: {Context}", JsonConvert.SerializeObject(terminal));

                            if (!string.IsNullOrEmpty(scenarioData))
                            {
                                PurchaseResponse response = GetScenarioDataResponse<PurchaseResponse>(scenarioData, _logger);

                                terminalResult.SetRn(response?.Rn ?? string.Empty);
                            }

                            _logger.LogInformation("RRN {Rrn}, InvoiceNum {InvoiceNum}, Rn {Rn}", terminalResult.Rrn, terminalResult.CheckNumber, terminalResult.Rn);
                        }
                        else
                        {
                            _logger.LogInformation("Успешно");
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(terminal.LastStatMsgDescription))
                    {
                        progress.Report(terminal.LastStatMsgDescription);

                        _logger.LogInformation("Сообщение терминала: {Message}", terminal.LastStatMsgDescription);
                    }

                    if (!string.IsNullOrEmpty(terminal.LastErrorDescription))
                    {
                        progress.Report(terminal.LastErrorDescription);

                        _logger.LogInformation("Ошибка терминала: {Message}", terminal.LastErrorDescription);

                        if (terminal.LastResult == 2 && terminal.LastErrorCode == 0)
                        {
                            await Task.Delay(1000, cancellationToken);

                            break;
                        }
                    }

                    if (terminal.LastResult == 1)
                    {
                        if (!string.IsNullOrEmpty(terminal.LastErrorDescription))
                        {
                            errors.Add(terminal.LastErrorDescription);
                        }
                        else if (!string.IsNullOrEmpty(terminal.LastStatMsgDescription))
                        {
                            errors.Add(terminal.LastStatMsgDescription);
                        }
                        else
                        {
                            errors.Add(UnknownError);
                        }
                    }

                    _logger.LogInformation("Check state finish: {Context}", JsonConvert.SerializeObject(terminal));
                } while ((terminal.LastResult == 2 && terminal.LastErrorCode != 0) || (terminal.LastResult == 0 && terminal.LastErrorCode == 0 && !isConfirmSend));

                await Task.Delay(500, default);

                terminal.ReqCurrReceipt();

                terminal.CommClose();

                _logger.LogInformation("Close connection: {Context}", JsonConvert.SerializeObject(terminal));
            }

            if (errors.Count == 0 && isConfirmSend)
            {
                return Result<TerminalPurchaseResult>.Success(terminalResult);
            }

            return Result<TerminalPurchaseResult>.Error("Error terminals transaction", errors);
        }

        public virtual Task<Result<ReturnRefundResult>> ReturnRefundAsync(decimal amount, bool fullAmountTransaction, string merchantId, string rn, string rrn, uint? invoiceNum, decimal? discount, DateTime? dateTimePuchase, IProgress<string> progress, CancellationToken cancellationToken)
        {
            return PurchaseReturnAsync(amount, merchantId, rn, progress, cancellationToken);
        }

        protected async Task<Result<ReturnRefundResult>> RefundAsync(decimal amount, decimal discount, string merchantId, string rrn, IProgress<string> progress, CancellationToken cancellationToken)
        {
            const string GeneralErrorMessage = "Failed to refund terminal payment";

            List<string> errors = new List<string>();
            ReturnRefundResult returnRefundResult = null;
            bool isConfirmSend = false;

            progress.Report("Начало отмены транзакции по RRN транзакции");

            _logger.LogInformation("Start function Refund for refund amount № transaction {Rrn}, amount {Amount}, discount {Discount}", rrn, amount, discount);

            if (byte.TryParse(merchantId, out byte merchant) && !string.IsNullOrEmpty(rrn))
            {
                IBPOS1Lib terminal = new BPOS1LibClass();

                _logger.LogInformation("Init: {Context}", JsonConvert.SerializeObject(terminal));

                terminal.CommOpenTCP(_ip, _port);

                _logger.LogInformation("Connect: {Context}", JsonConvert.SerializeObject(terminal));

                if (terminal.LastResult != 0)
                {
                    progress.Report(ReturnMoneyConnectionError);
                    errors.Add(ReturnMoneyConnectionError);
                }
                else
                {
                    terminal.SetErrorLang(2);

                    terminal.Refund((uint) (amount * 100), (uint) (discount * 100), merchant, rrn);

                    _logger.LogInformation("Refund: {Context}", JsonConvert.SerializeObject(terminal));

                    progress.Report("Ожидание карты");

                    do
                    {
                        _logger.LogInformation("Check state refund start: {Context}", JsonConvert.SerializeObject(terminal));

                        if (terminal.LastResult == 2)
                        {
                            await Task.Delay(500, cancellationToken);
                        }

                        if (terminal.LastResult == 0)
                        {
                            if (!isConfirmSend)
                            {
                                terminal.Confirm();
                                isConfirmSend = true;

                                returnRefundResult = new ReturnRefundResult(
                                    terminal.RRN,
                                    terminal.InvoiceNum,
                                    terminal.TerminalID,
                                    terminal.MerchantID,
                                    terminal.AuthCode,
                                    terminal.PAN,
                                    terminal.IssuerName);

                                _logger.LogInformation("Подтверждение операции (Confirm) отправлено: {Context}", JsonConvert.SerializeObject(terminal));
                            }
                            else
                            {
                                const string SuccessConfirm = "Подтверждение успешно";

                                _logger.LogInformation(SuccessConfirm);
                                progress.Report(SuccessConfirm);
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(terminal.LastStatMsgDescription))
                        {
                            progress.Report(terminal.LastStatMsgDescription);

                            _logger.LogInformation("LastStatMsgDescription: {Desc}", terminal.LastStatMsgDescription);
                        }

                        if (!string.IsNullOrEmpty(terminal.LastErrorDescription))
                        {
                            progress.Report(terminal.LastErrorDescription);

                            _logger.LogInformation("LastErrorDescription: {Desc}", terminal.LastErrorDescription);

                            if(terminal.LastResult == 2 && terminal.LastErrorCode == 0)
                            {
                                await Task.Delay(1000, cancellationToken);

                                break;
                            }
                        }

                        if (terminal.LastResult == 1)
                        {
                            if (!string.IsNullOrEmpty(terminal.LastErrorDescription))
                            {
                                errors.Add(terminal.LastErrorDescription);
                                _logger.LogError("LastErrorDescription: {Desc}", terminal.LastErrorDescription);
                            }
                            else if (!string.IsNullOrEmpty(terminal.LastStatMsgDescription))
                            {
                                errors.Add(terminal.LastStatMsgDescription);
                                _logger.LogError("LastStatMsgDescription: {Desc}", terminal.LastStatMsgDescription);
                            }
                            else
                            {
                                errors.Add(UnknownError);
                                _logger.LogError(UnknownError);
                            }
                        }

                        _logger.LogInformation("Check state finish: {Context}", JsonConvert.SerializeObject(terminal));

                    } while ((terminal.LastResult == 2 && terminal.LastErrorCode != 0) || (terminal.LastResult == 0 && terminal.LastErrorCode == 0 && !isConfirmSend));
                }

                terminal.CommClose();

                if (!isConfirmSend)
                {
                    errors.Add("Не было отправлено подтверждение");
                }
            }
            else
            {
                errors.Add("Не передан мерчант или RRN");
            }

            return errors.Count > 0
                ? Result<ReturnRefundResult>.Error(GeneralErrorMessage, errors)
                : Result.Success(returnRefundResult);
        }

        protected async Task<Result<ReturnRefundResult>> CancelAsync(uint invoiceNum, string merchantId, IProgress<string> progress, CancellationToken cancellationToken)
        {
            const string GeneralErrorMessage = "Failed to cancel terminal payment";

            List<string> errors = new List<string>();

            ReturnRefundResult returnRefundResult = null;

            progress.Report($"Начало отмены транзакции по номеру чека {invoiceNum}");

            _logger.LogInformation("Start function Cancel for refund amount invoice number {InvoiceNum}", invoiceNum);

            if (byte.TryParse(merchantId, out byte merchant))
            {
                IBPOS1Lib terminal = new BPOS1LibClass();

                _logger.LogInformation("Init: {Context}", JsonConvert.SerializeObject(terminal));

                terminal.CommOpenTCP(_ip, _port);

                _logger.LogInformation("Connect: {Context}", JsonConvert.SerializeObject(terminal));

                if (terminal.LastResult != 0)
                {
                    progress.Report(ReturnMoneyConnectionError);
                    errors.Add(ReturnMoneyConnectionError);
                    _logger.LogError(ReturnMoneyConnectionError);
                }
                else
                {
                    terminal.SetErrorLang(2);

                    terminal.Void(invoiceNum, merchant);

                    do
                    {
                        await Task.Delay(600, cancellationToken);

                        if (!string.IsNullOrEmpty(terminal.LastStatMsgDescription))
                        {
                            progress.Report(terminal.LastStatMsgDescription);

                            _logger.LogInformation("LastStatMsgDescription: {Desc}", terminal.LastErrorDescription);
                        }

                        if (terminal.LastResult == 1)
                        {
                            _logger.LogInformation("Void: {Context}", JsonConvert.SerializeObject(terminal));

                            if (!string.IsNullOrEmpty(terminal.LastErrorDescription))
                            {
                                errors.Add(terminal.LastErrorDescription);
                                _logger.LogError("LastErrorDescription: {Desc}", terminal.LastErrorDescription);
                            }
                            else if (!string.IsNullOrEmpty(terminal.LastStatMsgDescription))
                            {
                                errors.Add(terminal.LastStatMsgDescription);
                                _logger.LogError("LastStatMsgDescription {Desc}", terminal.LastStatMsgDescription);
                            }
                            else
                            {
                                errors.Add(UnknownError);
                                _logger.LogError(UnknownError);
                            }
                        }

                        if (terminal.LastResult == 0)
                        {
                            _logger.LogInformation("Successful refund transaction by check number");

                            returnRefundResult = new ReturnRefundResult(
                                terminal.RRN,
                                terminal.InvoiceNum,
                                terminal.TerminalID,
                                terminal.MerchantID,
                                terminal.AuthCode,
                                terminal.PAN,
                                terminal.IssuerName);

                            break;
                        }

                    } while (terminal.LastResult == 2);

                    await Task.Delay(600, cancellationToken);

                    _logger.LogInformation("start function ReqCurrReceipt");

                    terminal.ReqCurrReceipt();

                    terminal.CommClose();

                    _logger.LogInformation("Cancel refund amound by check number");

                    _logger.LogInformation("Void: {Context}", JsonConvert.SerializeObject(terminal));

                }
            }

            return errors.Count > 0
                ? Result<ReturnRefundResult>.Error(GeneralErrorMessage, errors)
                : Result.Success(returnRefundResult);
        }

        private async Task<Result<ReturnRefundResult>> PurchaseReturnAsync(decimal amount, string merchantId, string rn, IProgress<string> progress, CancellationToken cancellationToken)
        {
            const string GeneralErrorMessage = "Failed to refund terminal payment";

            List<string> errors = new List<string>();

            ReturnRefundResult returnResult = null;

            progress.Report("Начало отмены транзакции по RN");

            _logger.LogInformation("Start purchase return amount № transaction {Rn}, amount {Amount}", rn, amount);

            if (byte.TryParse(merchantId, out byte merchant) && !string.IsNullOrEmpty(rn))
            {
                string req = GetRequestPurchaseReturn(amount, merchant, rn, _logger);

                if (string.IsNullOrEmpty(req))
                {
                    errors.Add("Не сформирован запрос PurchaseReturn");

                    return Result<ReturnRefundResult>.Error(GeneralErrorMessage, errors);
                }

                IBPOS1Lib terminal = new BPOS1LibClass();

                _logger.LogInformation("Init: {Context}", JsonConvert.SerializeObject(terminal));

                terminal.CommOpenTCP(_ip, _port);

                _logger.LogInformation("Connect: {Context}", JsonConvert.SerializeObject(terminal));

                if (terminal.LastResult != 0)
                {
                    progress.Report("Ошибка связи с терминалом при выполнении возврата денег");
                    errors.Add("Ошибка связи с терминалом при выполнении возврата денег");
                }
                else
                {
                    _logger.LogInformation("StartScenarioRequest: {Context}", req);

                    terminal.StartScenario(1, req);

                    _logger.LogInformation("Check state refund start: {Context}", JsonConvert.SerializeObject(terminal));

                    do
                    {
                        _logger.LogInformation("Check state refund start: {Context}", JsonConvert.SerializeObject(terminal));

                        if (terminal.LastResult == 2)
                        {
                            await Task.Delay(500, cancellationToken);
                        }

                        if (terminal.LastResult == 0)
                        {
                            string result = terminal.ScenarioData;

                            if (!string.IsNullOrEmpty(result))
                            {
                                _logger.LogInformation("ScenarioData: {Context}", result);

                                ActionScenarioResponse actionScenarioResponse = GetScenarioDataResponse<ActionScenarioResponse>(result,_logger);

                                if (actionScenarioResponse?.ResultCode == "0" && actionScenarioResponse.Result?.Contains("УСПІШНА", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    returnResult = new ReturnRefundResult(
                                        terminal.RRN,
                                        terminal.InvoiceNum,
                                        terminal.TerminalID,
                                        terminal.MerchantID,
                                        terminal.AuthCode,
                                        terminal.PAN,
                                        terminal.IssuerName);

                                    break;
                                }
                            }

                            _logger.LogInformation("Check state refund start (LastResult == 0): {Context}", JsonConvert.SerializeObject(terminal));
                        }

                        if (!string.IsNullOrEmpty(terminal.LastStatMsgDescription))
                        {
                            progress.Report(terminal.LastStatMsgDescription);

                            _logger.LogInformation("LastStatMsgDescription: {Desc}", terminal.LastStatMsgDescription);
                        }

                        if (!string.IsNullOrEmpty(terminal.LastErrorDescription))
                        {
                            progress.Report(terminal.LastErrorDescription);

                            _logger.LogInformation("LastErrorDescription: {Desc}", terminal.LastErrorDescription);

                            if (terminal.LastResult == 2 && terminal.LastErrorCode == 0)
                            {
                                await Task.Delay(1000, cancellationToken);

                                break;
                            }
                        }

                        if (terminal.LastResult == 1)
                        {
                            if (!string.IsNullOrEmpty(terminal.LastErrorDescription))
                            {
                                errors.Add(terminal.LastErrorDescription);
                                _logger.LogError("LastErrorDescription: {Desc}", terminal.LastErrorDescription);
                            }
                            else if (!string.IsNullOrEmpty(terminal.LastStatMsgDescription))
                            {
                                errors.Add(terminal.LastStatMsgDescription);
                                _logger.LogError("LastStatMsgDescription: {Desc}", terminal.LastStatMsgDescription);
                            }
                            else
                            {
                                errors.Add(UnknownError);
                                _logger.LogError(UnknownError);
                            }
                        }

                        _logger.LogInformation("Check state finish: {Context}", JsonConvert.SerializeObject(terminal));

                    } while ((terminal.LastResult == 2 && terminal.LastErrorCode != 0) || (terminal.LastResult == 0 && terminal.LastErrorCode == 0));

                    terminal.CommClose();
                }
            }
            else
            {
                errors.Add("Не передан мерчант или RN");
            }

            return errors.Count > 0
                ? Result<ReturnRefundResult>.Error(GeneralErrorMessage, errors)
                : Result.Success(returnResult);
        }

        public void ReConnect()
        {
        }

        private T GetScenarioDataResponse<T>(string response, ILogger<IngenicoPosTerminalClient> logger)
            where T: class
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));

                using (TextReader reader = new StringReader(response))
                {
                    T result = (T)serializer.Deserialize(reader);

                    return result;
                }
            }
            catch (Exception e)
            {
                logger.LogError("GetRn: {error}", e.ToString());

                return null;
            }
        }

        private string GetRequestPurchaseReturn(decimal amount, uint merchant, string rn, ILogger<IngenicoPosTerminalClient> logger)
        {
            string req = string.Empty;

            try
            {
                XmlWriterSettings settings = new XmlWriterSettings
                {
                    Indent = true,
                    OmitXmlDeclaration = true
                };

                ActionScenarioRequest request = new ActionScenarioRequest()
                {
                    Action = "PurchaseReturn",
                    Amount = (uint) (amount * 100),
                    MerchantId = merchant,
                    Rn = rn
                };

                var ns = new XmlSerializerNamespaces(new[] {XmlQualifiedName.Empty});

                using (var stream = new StringWriter())
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    var serializer = new XmlSerializer(typeof(ActionScenarioRequest));
                    serializer.Serialize(writer, request, ns);
                    req = stream.ToString();
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "GetRequestPurchaseReturn: {error}", e.ToString());
            }

            return req;
        }
    }
}