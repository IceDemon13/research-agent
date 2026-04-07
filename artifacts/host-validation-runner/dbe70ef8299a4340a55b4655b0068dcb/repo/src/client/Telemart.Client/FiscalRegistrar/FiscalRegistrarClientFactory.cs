using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Data.Options;
using Telemart.Client.Data.Requests.Features.FiscalRegistrar;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Client.Validators;
using Telemart.Common.ErrorHandling;
using Telemart.Common.Extensions;

namespace Telemart.Client.FiscalRegistrar
{
    public class FiscalRegistrarClientFactory : IFiscalRegistrarClientFactory
    {
        private readonly IDictionaries _dictionaries;
        private readonly IWebClient _webClient;
        private readonly IFiscalSettingsValidator _fiscalSettingsValidator;
        private readonly IEquipmentSettingsStore _equipmentSettingsStore;
        private readonly IOptionsSnapshot<FiscalOptions> _options;
        private readonly IServiceProvider _serviceProvider;

        public FiscalRegistrarClientFactory(
            IDictionaries dictionaries,
            IWebClient webClient,
            IFiscalSettingsValidator fiscalSettingsValidator,
            IEquipmentSettingsStore equipmentSettingsStore,
            IOptionsSnapshot<FiscalOptions> options,
            IServiceProvider serviceProvider)
        {
            _dictionaries = dictionaries;
            _webClient = webClient;
            _fiscalSettingsValidator = fiscalSettingsValidator;
            _equipmentSettingsStore = equipmentSettingsStore;
            _options = options;
            _serviceProvider = serviceProvider;
        }

        public async Task<Result<IFiscalRegistrarClient>> CreateAsync(CancellationToken cancellationToken)
        {
            EquipmentSettingsInfo equipmentSettings = await _equipmentSettingsStore.LoadAsync();

            Guid uniqueDeviceGuid = equipmentSettings.UniqueDeviceGuid.Value;

            List<FiscalRegistrarSettingsDto> fiscalSettings = await _webClient.ExecuteApiRequestAsync(new QueryFiscalRegistrarSettings(uniqueDeviceGuid.ToString(), true));

            Result<IReadOnlyCollection<FiscalRegistrarSettingsDto>> fiscalSettingsValidationResult = await _fiscalSettingsValidator.ValidateAsync(fiscalSettings);

            if (fiscalSettingsValidationResult.IsSuccess)
            {
                if (fiscalSettingsValidationResult.Data.Count > 1)
                {
                    return Result.Error<IFiscalRegistrarClient>("Failed to validate fiscal settings", "Найдено больше одной настройки РРО");
                }

                return Result.Success(await CreateAsync(fiscalSettingsValidationResult.Data.First(), cancellationToken));
            }

            return fiscalSettingsValidationResult.CastError<IFiscalRegistrarClient>();
        }

        public async Task<IFiscalRegistrarClient> CreateAsync(FiscalRegistrarSettingsDto settings, CancellationToken cancellationToken)
        {
            FiscalRegistrarType type = _dictionaries.GetItemById<FiscalRegistrarType>(settings.FiscalConnectionTypeId);

            if (type == null)
            {
                throw new Exception("Fiscal type not support");
            }

            return await CreateAsync(
                type.Type,
                settings,
                cancellationToken);
        }

        public Task<IFiscalRegistrarClient> CreateAsync(Type type, FiscalRegistrarSettingsDto settings, CancellationToken cancellationToken)
        {
            IFiscalRegistrarClient fiscalRegistrarClient = (IFiscalRegistrarClient)_serviceProvider.GetRequiredService(type);

            switch (fiscalRegistrarClient)
            {
                case FiscalRegistrarClient hardwareFiscalRegistrarClient:

                    int delayInterval = _options.Value.DelayMs;

                    hardwareFiscalRegistrarClient.SetDelayInterval(delayInterval);

                    break;
            }

            fiscalRegistrarClient.Settings = settings;

            return Task.FromResult(fiscalRegistrarClient);
        }
    }
}