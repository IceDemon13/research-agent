using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Validators
{
    public class PosSettingsValidator : IPosSettingsValidator
    {
        private readonly IWebClient _webClient;
        private readonly TerminalOptions _options;

        public PosSettingsValidator(IWebClient webClient, TerminalOptions options)
        {
            _webClient = webClient;
            _options = options;
        }

        public async Task<Result<IReadOnlyCollection<PosSettingsDto>>> ValidateAsync(IReadOnlyCollection<PosSettingsDto> posSettings)
        {
            List<string> errors = new List<string>();
            List<PosSettingsDto> validPosTerminals = new List<PosSettingsDto>();

            List<CashboxDto> cashboxes = await _webClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            int[] duplicateCashboxIds = posSettings.GroupBy(x => x.CashboxId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            foreach (PosSettingsDto posSetting in posSettings)
            {
                List<string> terminalErrors = new List<string>();
                CashboxDto cashbox = cashboxes.FirstOrDefault(x => x.Id == posSetting.CashboxId);

                if (cashbox == null)
                {
                    terminalErrors.Add($"Касса {posSetting.CashboxId} не найдена");
                    continue;
                }

                if (duplicateCashboxIds.Contains(posSetting.CashboxId))
                {
                    terminalErrors.Add($"Терминал с кассой \"{cashbox.Name}\" ({posSetting.IpAddress}): касса используется в нескольких терминалах");
                }

                if (posSetting.LegalEntityId == null)
                {
                    terminalErrors.Add($"Терминал с кассой \"{cashbox.Name}\" ({posSetting.IpAddress}) не заполнено юр. лицо");
                }
                else
                {
                    if (posSetting.LegalEntityId != cashbox.LegalEntityId)
                    {
                        List<LegalEntityDto> legalEntities = await _webClient.ExecuteApiRequestAsync(new QueryLegalEntities(), true);

                        IReadOnlyDictionary<int, string> legalEntityNames = legalEntities.ToDictionary(x => x.Id, x => x.Name);

                        terminalErrors.Add($"Терминал с кассой \"{cashbox.Name}\" ({posSetting.IpAddress}). Юр. лицо терминала ({legalEntityNames.GetValueOrDefault(posSetting.LegalEntityId ?? 0)}) не равно юр. лицу кассы ({legalEntityNames.GetValueOrDefault(cashbox.LegalEntityId ?? 0)})");
                    }
                }

                if (!int.TryParse(posSetting.Merchant, out _))
                {
                    terminalErrors.Add($"Терминал с кассой \"{cashbox.Name}\" ({posSetting.IpAddress}). Мерчант \"{posSetting.Merchant}\" должен быть числом");
                }

                if (IPEndPoint.TryParse(posSetting.IpAddress, out IPEndPoint ipAddress))
                {
                    if (_options.ValidateMac)
                    {
                        string mac = ipAddress.Address.GetMacByIp();

                        if (string.IsNullOrEmpty(mac))
                        {
                            terminalErrors.Add(
                                $"Терминал с кассой \"{cashbox.Name}\" ({posSetting.IpAddress}) не найден в вашей сети");
                        }
                        else if (mac != posSetting.MacAddress)
                        {
                            terminalErrors.Add(
                                $"MAC аддресс терминала с кассой \"{cashbox.Name}\" ({posSetting.IpAddress}) MAC: \"{posSetting.MacAddress}\" не соответствует найденному устройству в сети \"{mac}\"");
                        }
                    }
                }
                else
                {
                    terminalErrors.Add($"Терминал с кассой \"{cashbox.Name}\": IP \"{posSetting.IpAddress}\" не валидный");
                }

                if (terminalErrors.Any())
                {
                    errors.AddRange(terminalErrors);
                }
                else
                {
                    validPosTerminals.Add(posSetting);
                }
            }

            if (validPosTerminals.Any())
            {
                return Result.Success<IReadOnlyCollection<PosSettingsDto>>(validPosTerminals, errors);
            }
            else
            {
                if (errors.Count == 0)
                {
                    errors.Add("Нет доступных настроек терминалов");
                }

                return Result.Error<IReadOnlyCollection<PosSettingsDto>>("Failed to validate POS terminals", errors);
            }
        }
    }
}