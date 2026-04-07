using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.FiscalRegistrar;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Client.Validators;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;

namespace Telemart.Client.Common.Settings.Equipment
{
    public sealed class FiscalRegistrarSettingsChecker : IFiscalRegistrarSettingsChecker
    {
        private readonly IWebClient _webClient;
        private readonly IFiscalSettingsValidator _fiscalSettingsValidator;
        private Guid _uniqueDevaceId;
        private volatile bool _isCreatedPosSettings;

        public FiscalRegistrarSettingsChecker(
            IWebClient webClient,
            IFiscalSettingsValidator fiscalSettingsValidator)
        {
            _webClient = webClient;
            _fiscalSettingsValidator = fiscalSettingsValidator;
        }

        public async Task<Result<bool>> CheckFiscalRegistrarSettingAsync(Guid? uniqueDeviceGuid, FiscalRegistrarSettingsInfo rroSettingsInfo)
        {
            _uniqueDevaceId = uniqueDeviceGuid ?? throw new ArgumentNullException("Уникальный гуид компьютера не был создан.");

            List<FiscalRegistrarSettingsDto> fiscalSettingsDtos = await _webClient.ExecuteApiRequestAsync(new QueryFiscalRegistrarSettings(_uniqueDevaceId.ToString()));

            IReadOnlyCollection<string> resultRroSettingsValidating = await FiscalRegistrarSettingsValidateAsync(fiscalSettingsDtos);

            IReadOnlyCollection<string> resultFiscalRegistrarCompare = await FiscalRegistrarCompareAsync(rroSettingsInfo, fiscalSettingsDtos);

            List<string> errors = new List<string>();

            errors.AddRange(resultRroSettingsValidating.ToList());
            errors.AddRange(resultFiscalRegistrarCompare.ToList());

            if (errors.Any())
            {
                return Result<bool>.Error("failed check pos settings", errors.ToArray());
            }

            return Result.Success(_isCreatedPosSettings);
        }

        private async Task<IReadOnlyCollection<string>> FiscalRegistrarSettingsValidateAsync(IReadOnlyCollection<FiscalRegistrarSettingsDto> fiscalRegistrarSettings)
        {
            if (fiscalRegistrarSettings?.Count > 0)
            {
                Result<IReadOnlyCollection<FiscalRegistrarSettingsDto>> fiscalValidationResult = await _fiscalSettingsValidator.ValidateAsync(fiscalRegistrarSettings);

                if (!fiscalValidationResult.IsSuccess)
                {
                    return fiscalValidationResult.ErrorObj.GetMessages()
                        .ToReadOnlyObservableCollection();
                }
                else if (fiscalValidationResult.Warnings?.Any() == true)
                {
                    return fiscalValidationResult.Warnings
                        .ToReadOnlyObservableCollection();
                }
            }

            return Array.Empty<string>();
        }

        private async Task<IReadOnlyCollection<string>> FiscalRegistrarCompareAsync(FiscalRegistrarSettingsInfo rroSettingsInfo, IReadOnlyCollection<FiscalRegistrarSettingsDto> fiscalRegistrarSettings)
        {
            if (rroSettingsInfo == null || !rroSettingsInfo.FiscalRegistrarIsValid())
            {
                return Array.Empty<string>();
            }

            FiscalRegistrarSettingsDto rroDto = fiscalRegistrarSettings.FirstOrDefault(x => x.CashboxId == rroSettingsInfo.CashboxId);

            if (rroDto == null)
            {
                return await CreateFiscalRegistrarSettingsAsync(
                    rroSettingsInfo.Type == FiscalRegistrarType.Software.Name ? FiscalRegistrarType.SoftwareId : FiscalRegistrarType.HardwareId,
                    rroSettingsInfo.CashboxId.Value,
                    rroSettingsInfo.User,
                    rroSettingsInfo.Password,
                    rroSettingsInfo.Ip);
            }

            if (rroDto.Login == rroSettingsInfo.User
                && rroDto.Password == rroSettingsInfo.Password
                && rroDto.IpAddress == rroSettingsInfo.Ip
                && rroDto.FiscalConnectionTypeId.ToString() == rroSettingsInfo.Type)
            {
                _isCreatedPosSettings = true;

                return Array.Empty<string>();
            }

            return new[] { "Настройки РРО в файле отличаются от настроек в базе" };
        }

        private async Task<IReadOnlyCollection<string>> CreateFiscalRegistrarSettingsAsync(
            int rroConnectTypeId,
            int cashboxId,
            string user,
            string password,
            string ip)
        {
            string macAddress = GetMac(ip);

            if (rroConnectTypeId == FiscalRegistrarType.HardwareId && string.IsNullOrEmpty(macAddress))
            {
                return new[] { $"Мак-адрес РРО не определен. Возможно в файле настроек указан невалидный IP {ip}" };
            }

            CreateFiscalRegistrarSettingsDto createDto = new CreateFiscalRegistrarSettingsDto(
                rroConnectTypeId,
                cashboxId,
                null,
                user,
                password,
                _uniqueDevaceId.ToString(),
                ip,
                macAddress);

            try
            {
                Result<FiscalRegistrarSettingsDto> result = await _webClient.ExecuteApiRequestAsync(new CreateFiscalRegitrarSettings(createDto));

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
                return ex.GetErrorItems().Select(x => $"Ошибка настройки РРО: {x.Message}").ToArray();
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