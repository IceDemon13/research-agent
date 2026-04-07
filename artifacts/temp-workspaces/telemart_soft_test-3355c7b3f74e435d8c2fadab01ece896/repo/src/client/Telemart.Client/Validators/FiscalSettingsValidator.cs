using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Validators
{
    public class FiscalSettingsValidator : IFiscalSettingsValidator
    {
        private readonly IWebClient _webClient;

        public FiscalSettingsValidator(IWebClient webClient)
        {
            _webClient = webClient;
        }

        public async Task<Result<IReadOnlyCollection<FiscalRegistrarSettingsDto>>> ValidateAsync(IReadOnlyCollection<FiscalRegistrarSettingsDto> fiscalRegistrarSettings)
        {
            List<string> errors = new List<string>();
            List<FiscalRegistrarSettingsDto> validRegistrators = new List<FiscalRegistrarSettingsDto>();

            int[] duplicateCashboxIds = fiscalRegistrarSettings.GroupBy(x => x.CashboxId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            List<CashboxDto> cashboxes = await _webClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            foreach (FiscalRegistrarSettingsDto fiscalRegistrarSetting in fiscalRegistrarSettings)
            {
                List<string> rroErrors = new List<string>();
                CashboxDto cashbox = cashboxes.FirstOrDefault(x => x.Id == fiscalRegistrarSetting.CashboxId);

                if (cashbox == null)
                {
                    rroErrors.Add($"Касса {fiscalRegistrarSetting.CashboxId} не найдена");
                    continue;
                }

                if (duplicateCashboxIds.Contains(fiscalRegistrarSetting.CashboxId))
                {
                    rroErrors.Add($"РРО с кассой \"{cashbox.Name}\" ({fiscalRegistrarSetting.IpAddress}): касса используется в нескольких РРО");
                }

                if (fiscalRegistrarSetting.LegalEntityId == null)
                {
                    rroErrors.Add($"РРО с кассой \"{cashbox.Name}\" ({fiscalRegistrarSetting.IpAddress}) не заполнено юр. лицо");
                }
                else
                {
                    if (fiscalRegistrarSetting.LegalEntityId != cashbox.LegalEntityId)
                    {
                        List<LegalEntityDto> legalEntities = await _webClient.ExecuteApiRequestAsync(new QueryLegalEntities(), true);

                        IReadOnlyDictionary<int, string> legalEntityNames = legalEntities.ToDictionary(x => x.Id, x => x.Name);

                        rroErrors.Add($"РРО с кассой \"{cashbox.Name}\" ({fiscalRegistrarSetting.IpAddress}). Юр. лицо РРО ({legalEntityNames.GetValueOrDefault(fiscalRegistrarSetting.LegalEntityId ?? 0)}) не равно юр. лицу кассы ({legalEntityNames.GetValueOrDefault(cashbox.LegalEntityId ?? 0)})");
                    }
                }

                if (fiscalRegistrarSetting.FiscalConnectionTypeId == FiscalConnectionType.HardwareId)
                {
                    if (IPAddress.TryParse(fiscalRegistrarSetting.IpAddress, out IPAddress ipAddress))
                    {
                        string mac = ipAddress.GetMacByIp();

                        if (string.IsNullOrEmpty(mac))
                        {
                            rroErrors.Add($"РРО с кассой \"{cashbox.Name}\" ({fiscalRegistrarSetting.IpAddress}) не найден в вашей сети");
                        }
                        else if (mac != fiscalRegistrarSetting.MacAddress)
                        {
                            rroErrors.Add($"MAC аддресс РРО с кассой \"{cashbox.Name}\" ({fiscalRegistrarSetting.IpAddress}) MAC: \"{fiscalRegistrarSetting.MacAddress}\" не соответствует найденному устройству в сети \"{mac}\"");
                        }
                    }
                    else
                    {
                        rroErrors.Add($"РРО с кассой \"{cashbox.Name}\": IP \"{fiscalRegistrarSetting.IpAddress}\" не валидный");
                    }

                    if (string.IsNullOrEmpty(fiscalRegistrarSetting.Login))
                    {
                        rroErrors.Add($"РРО с кассой \"{cashbox.Name}\" ({fiscalRegistrarSetting.IpAddress}): логин не заполнен");
                    }

                    if (string.IsNullOrEmpty(fiscalRegistrarSetting.Password))
                    {
                        rroErrors.Add($"РРО с кассой \"{cashbox.Name}\" ({fiscalRegistrarSetting.IpAddress}): пароль не заполнен");
                    }
                }

                if (rroErrors.Any())
                {
                    errors.AddRange(rroErrors);
                }
                else
                {
                    validRegistrators.Add(fiscalRegistrarSetting);
                }
            }

            if (validRegistrators.Any())
            {
                return Result.Success<IReadOnlyCollection<FiscalRegistrarSettingsDto>>(validRegistrators, errors);
            }
            else
            {
                if (errors.Count == 0)
                {
                    errors.Add("Нет доступных настроек РРО");
                }

                return Result.Error<IReadOnlyCollection<FiscalRegistrarSettingsDto>>("Failed to validate RRO settings", errors);
            }
        }
    }
}