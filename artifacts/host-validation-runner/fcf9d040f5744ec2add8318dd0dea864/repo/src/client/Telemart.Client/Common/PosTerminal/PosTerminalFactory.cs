using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Data.Requests.Features.PosTerminal;
using Telemart.Client.Data.WebClient;
using Telemart.Client.PosTerminal.Ingenico;
using Telemart.Client.PosTerminal.PrivatBank;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Client.Validators;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;

namespace Telemart.Client.Common.PosTerminal
{
    public class PosTerminalFactory : IPosTerminalFactory
    {
        private readonly IPrivatBankPosTerminalClient _privatTerminal;
        private readonly IWebClient _webClient;
        private readonly IPosSettingsValidator _posSettingsValidator;
        private readonly IEquipmentSettingsStore _equipmentSettingsStore;
        private readonly IServiceProvider _serviceProvider;

        public PosTerminalFactory(
            IPrivatBankPosTerminalClient privatTerminal,
            IWebClient webClient,
            IPosSettingsValidator posSettingsValidator,
            IEquipmentSettingsStore equipmentSettingsStore,
            IServiceProvider serviceProvider)
        {
            _privatTerminal = privatTerminal;
            _webClient = webClient;
            _posSettingsValidator = posSettingsValidator;
            _equipmentSettingsStore = equipmentSettingsStore;
            _serviceProvider = serviceProvider;
        }

        public async Task<Result<IPosTerminalClient>> CreateAsync(int legalEntityId)
        {
            EquipmentSettingsInfo equipmentSettings = await _equipmentSettingsStore.LoadAsync();

            Guid uniqueDeviceGuid = equipmentSettings.UniqueDeviceGuid.Value;

            List<PosSettingsDto> posSettings = await _webClient.ExecuteApiRequestAsync(new QueryPosSettings(uniqueDeviceGuid.ToString()));

            posSettings = posSettings.Where(x => x.LegalEntityId == legalEntityId).ToList();

            Result<IReadOnlyCollection<PosSettingsDto>> posSettingsValidationResult = await _posSettingsValidator.ValidateAsync(posSettings);

            if (posSettingsValidationResult.IsSuccess)
            {
                if (posSettingsValidationResult.Data.Count > 1)
                {
                    return Result.Error<IPosTerminalClient>("Failed to validate fiscal settings", "Найдено больше одной настройки терминала");
                }
                else
                {
                    return Result.Success(await CreateAsync(posSettingsValidationResult.Data.First()));
                }
            }
            else
            {
                return posSettingsValidationResult.CastError<IPosTerminalClient>();
            }
        }

        public Task<IPosTerminalClient> CreateAsync(PosSettingsDto settings)
        {
            PosType type = (PosType)settings.PosTypeId;

            string ipStr = settings.IpAddress;

            if (string.IsNullOrEmpty(ipStr))
            {
                return Task.FromResult<IPosTerminalClient>(null);
            }

            return Task.FromResult(Create(type, ipStr, settings));
        }

        public IPosTerminalClient Create(PosType type, string ip, PosSettingsDto settings = null)
        {
            IPosTerminalClient client;
            IPEndPoint ipEndPoint;

            switch (type)
            {
                case PosType.Ingenico:
                    _privatTerminal?.Kill();
                    client = _serviceProvider.GetRequiredService<IngenicoPosTerminalClient>();
                    ipEndPoint = IPEndPoint.Parse(ip);

                    client.Connect(ipEndPoint.Address.ToString(), ipEndPoint.Port == 0 ? 2000 : ipEndPoint.Port);
                    break;
                case PosType.PrivatBank:
                    client = _privatTerminal;

                    if (!client.IsConnected())
                    {
                        ipEndPoint = IPEndPoint.Parse(ip);

                        client.Connect(ipEndPoint.Address.ToString(), ipEndPoint.Port == 0 ? 2000 : ipEndPoint.Port);
                    }
                    break;
                case PosType.Ukrsibbank:
                    _privatTerminal?.Kill();
                    client = _serviceProvider.GetRequiredService<IngenicoUkrsibbankPosTerminalClient>();
                    ipEndPoint = IPEndPoint.Parse(ip);

                    client.Connect(ipEndPoint.Address.ToString(), ipEndPoint.Port == 0 ? 2100 : ipEndPoint.Port);
                    break;

                default: throw new NotSupportedException("Terminal type not supported");
            }

            client.Settings = settings;

            return client;
        }
    }
}