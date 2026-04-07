using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.PosTerminal;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Client.Validators;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;

namespace Telemart.Client.Common.Settings.Equipment
{
    public class PosSettingsChecker : IPosSettingsChecker
    {
        private readonly IWebClient _webClient;
        private readonly IPosSettingsValidator _posSettingsValidator;
        private readonly TerminalOptions _terminalOptions;
        private Guid _uniqueDevaceGuid;
        private volatile bool _isCreatedPosSettings;

        public PosSettingsChecker(
            IWebClient webClient,
            IPosSettingsValidator posSettingsValidator,
            TerminalOptions terminalOptions)
        {
            _webClient = webClient;
            _posSettingsValidator = posSettingsValidator;
            _terminalOptions = terminalOptions;
        }

        public async Task<Result<bool>> CheckPosSettingAsync(Guid? uniqueDeviceGuid, PosSettingsInfo posInfo)
        {
            _uniqueDevaceGuid = uniqueDeviceGuid ?? throw new ArgumentNullException("Уникальный гуид компьютера не был создан.");

            List<PosSettingsDto> posSettingsDtos = await _webClient.ExecuteApiRequestAsync(new QueryPosSettings(_uniqueDevaceGuid.ToString()));

            IReadOnlyCollection<string> resultPosSetingsValidating = await PosSettingsValidateAsync(posSettingsDtos);

            IReadOnlyCollection<string> resultPosSettingsCompare = await PosSettingsCompareAsync(posInfo, posSettingsDtos);

            List<string> errors = new List<string>();

            errors.AddRange(resultPosSetingsValidating.ToList());
            errors.AddRange(resultPosSettingsCompare.ToList());

            if (errors.Any())
            {
                return Result<bool>.Error("failed check pos settings", errors.ToArray());
            }

            return Result.Success(_isCreatedPosSettings);
        }

        private async Task<IReadOnlyCollection<string>> PosSettingsValidateAsync(IReadOnlyCollection<PosSettingsDto> posSettingsDtos)
        {
            if (posSettingsDtos?.Count > 0)
            {
                Result<IReadOnlyCollection<PosSettingsDto>> result = await _posSettingsValidator.ValidateAsync(posSettingsDtos);

                if (!result.IsSuccess)
                {
                    return result.ErrorObj.GetMessages()
                        .ToReadOnlyObservableCollection();
                }
                else if (result.Warnings?.Any() == true)
                {
                    return result.Warnings
                        .ToReadOnlyObservableCollection();
                }
            }

            return Array.Empty<string>();
        }

        private async Task<IReadOnlyCollection<string>> PosSettingsCompareAsync(PosSettingsInfo posInfo, IReadOnlyCollection<PosSettingsDto> posSettingsDto)
        {
            if (posInfo == null || !posInfo.PosIsValid())
            {
                return Array.Empty<string>();
            }

            PosSettingsDto posDto = posSettingsDto.FirstOrDefault(x => x.CashboxId == posInfo.CashboxId);

            if (posDto == null)
            {
                return await CreatePosSettingsAsync(
                    (int)posInfo.Type.Value,
                    posInfo.CashboxId.Value,
                    posInfo.Merchant,
                    posInfo.Ip);
            }

            if (posDto.Merchant == posInfo.Merchant
               && posDto.IpAddress == posInfo.Ip
               && (PosType)posDto.PosTypeId == posInfo.Type)
            {
                _isCreatedPosSettings = true;

                return Array.Empty<string>();
            }

            return new[] { "Настройки РРО в файле отличаются от настроек в базе" };
        }

        private async Task<IReadOnlyCollection<string>> CreatePosSettingsAsync(
            int posTypeId,
            int cashboxId,
            string mechant,
            string ip)
        {
            string macAddress = GetMac(ip);

            if (_terminalOptions.ValidateMac && string.IsNullOrEmpty(macAddress))
            {
                return new[]
                    { $"Мак-адрес терминала не определен. Возможно в файле настроек указан невалидный IP {ip}" };
            }

            CreatePosSettingsDto createDto = new CreatePosSettingsDto(
                posTypeId,
                cashboxId,
                null,
                mechant,
                _uniqueDevaceGuid.ToString(),
                macAddress,
                ip,
                true);

            try
            {
                Result<PosSettingsDto> result = await _webClient.ExecuteApiRequestAsync(new CreatePosSettings(createDto));

                if (result.IsSuccess)
                {
                    _isCreatedPosSettings = true;

                    return Array.Empty<string>();
                }

                return result.ErrorObj.GetMessages()
                    .ToReadOnlyObservableCollection();
            }
            catch (UnexpectedSatusException ex)
            {
                return ex.GetErrorItems().Select(x => $"Ошибка настройки терминала: {x.Message}").ToArray();
            }
        }

        private string GetMac(string ip)
        {
            if (IPAddress.TryParse(ip, out IPAddress ipAddress))
            {
                string mac = ipAddress.GetMacByIp();

                if (mac == "00-00-00-00-00-00")
                {
                    return null;
                }

                return mac;
            }

            return null;
        }
    }
}