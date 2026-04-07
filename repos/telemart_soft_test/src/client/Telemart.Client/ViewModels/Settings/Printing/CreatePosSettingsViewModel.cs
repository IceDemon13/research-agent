using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.Requests.Features.PosTerminal;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Settings.Printing
{
    public sealed class CreatePosSettingsViewModel : TelemartDialogViewModelBase
    {
        public CreatePosSettingsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            Messenger = messenger;
        }

        public ReadOnlyObservableCollection<PosTerminalType> PosTypes
        {
            get { return GetProperty(() => PosTypes); }
            set { SetProperty(() => PosTypes, value); }
        }

        public ReadOnlyObservableCollection<CashboxDto> PosCashboxes
        {
            get { return GetProperty(() => PosCashboxes); }
            private set { SetProperty(() => PosCashboxes, value); }
        }

        public int PosTypeId
        {
            get { return GetProperty(() => PosTypeId); }
            set { SetProperty(() => PosTypeId, value, PosTypeChanged); }
        }

        public CashboxDto PosCashbox
        {
            get { return GetProperty(() => PosCashbox); }
            set { SetProperty(() => PosCashbox, value); }
        }

        public string PosMerchant
        {
            get { return GetProperty(() => PosMerchant); }
            set { SetProperty(() => PosMerchant, value); }
        }

        public string PosIp
        {
            get { return GetProperty(() => PosIp); }
            set { SetProperty(() => PosIp, value, OnPosIpChanged); }
        }

        public string Port
        {
            get { return GetProperty(() => Port); }
            set { SetProperty(() => Port, value); }
        }

        public string PosMacAddress
        {
            get { return GetProperty(() => PosMacAddress); }
            set { SetProperty(() => PosMacAddress, value); }
        }

        public string UniqueDeviceId
        {
            get { return GetProperty(() => UniqueDeviceId); }
            private set { SetProperty(() => UniqueDeviceId, value); }
        }

        public int? LegalEntityId
        {
            get { return GetProperty(() => LegalEntityId); }
            private set { SetProperty(() => LegalEntityId, value); }
        }

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<CreatePosSettingsViewModel> builder)
        {
            builder.Property(x => x.PosTypeId)
                .MatchesInstanceRule(
                    (x, y) => x > 0,
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.PosCashbox)
                .MatchesInstanceRule(
                    (x, y) => x != null,
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.PosMerchant)
                .MatchesInstanceRule(
                    (x, y) => !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.PosIp)
                .MatchesInstanceRule((x, y) => !string.IsNullOrEmpty(x), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            PosParameter parameter = Parameter as PosParameter;

            UniqueDeviceId = parameter?.UniqueDeviceId;

            LegalEntityId = parameter?.LegalEntityId;

            Title = "Создание настроек терминала";

            PosTypes = Dictionaries.GetItems<PosTerminalType>().ToReadOnlyObservableCollection();

            await LoadCashboxesAsync();
        }

        protected override async Task HandleOkAsync()
        {
            CreatePosSettingsDto createDto = new CreatePosSettingsDto(
                PosTypeId,
                PosCashbox.Id,
                null,
                PosMerchant,
                UniqueDeviceId,
                PosMacAddress,
                string.IsNullOrEmpty(Port) ? PosIp : $"{PosIp}:{Port}");

            Result<PosSettingsDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreatePosSettings(createDto)),
                "создании настроек терминала",
                "Настройки терминала сохранены",
                this,
                true,
                true,
                confirmText: "Создать настройки терминала");

            if (result.IsSuccess && result.Data != null)
            {
                Messenger.Send(new PosSettingsMessage(result.Data, MessageType.Added));

                CloseOk();
            }

            Close();
        }

        private async Task LoadCashboxesAsync()
        {
            List<CashboxDto> cashboxes = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true),
                "получении списка касс",
                null,
                this,
                true,
                showNotification: false);

            ReadOnlyObservableCollection<int> grayLegalEntitiesIds = await GetGrayLegalEntityIdsAsync();

            PosCashboxes = cashboxes
                .Where(x =>
                    x.IsActive
                    && x.AllowedPayments.Contains(Payment.TerminalId)
                    && WebClient.AuthenticatedEmployee.AllowCashboxes.Contains(x.Id)
                    && x.LegalEntityId != null
                    && (x.LegalEntityId == LegalEntityId
                        || grayLegalEntitiesIds.Contains(x.LegalEntityId.Value)))
                .ToReadOnlyObservableCollection();
        }

        private async Task<ReadOnlyObservableCollection<int>> GetGrayLegalEntityIdsAsync()
        {
            ReadOnlyObservableCollection<int> grayLegalEntityIds = Array.Empty<int>().ToReadOnlyObservableCollection();

            List<LegalEntityDto> legalEntities = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryLegalEntities(), true),
                "получении списка юр. лиц",
                null,
                this,
                true,
                showNotification: false);

            if (legalEntities != null)
            {
                grayLegalEntityIds = legalEntities
                    .Where(x => x.White == false)
                    .Select(x => x.Id)
                    .ToReadOnlyObservableCollection();
            }

            return grayLegalEntityIds;
        }

        private void OnPosIpChanged()
        {
            PosMacAddress = GetMac();

            RaisePropertiesChanged(nameof(PosMacAddress));
        }

        private string GetMac()
        {
            if (IPAddress.TryParse(PosIp, out IPAddress ipAddress))
            {
                return ipAddress.GetMacByIp();
            }

            return null;
        }

        private void PosTypeChanged()
        {
            Port = PosTypeId switch
            {
                PosTerminalType.IngenicoId => "2000",
                PosTerminalType.PrivatBankId => "2000",
                PosTerminalType.UkrsibbankId => "2100",
                _ => null
            };
        }
    }
}