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
using Telemart.Client.Data.Requests.Features.FiscalRegistrar;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Settings.Printing
{
    public sealed class CreateFiscalRegistrarSettingsViewModel : TelemartDialogViewModelBase
    {
        public CreateFiscalRegistrarSettingsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler ?? throw new ArgumentException(nameof(errorHandler));

            Messenger = messenger ?? throw new ArgumentException(nameof(messenger));
        }

        public ReadOnlyObservableCollection<CashboxDto> FiscalRegistrarCashboxes
        {
            get { return GetProperty(() => FiscalRegistrarCashboxes); }
            private set { SetProperty(() => FiscalRegistrarCashboxes, value); }
        }

        public ReadOnlyObservableCollection<FiscalConnectionTypeDto> FiscalRegistrarTypes
        {
            get { return GetProperty(() => FiscalRegistrarTypes); }
            set { SetProperty(() => FiscalRegistrarTypes, value); }
        }

        public int FiscalRegistrarConnectTypeId
        {
            get { return GetProperty(() => FiscalRegistrarConnectTypeId); }
            set { SetProperty(() => FiscalRegistrarConnectTypeId, value, OnFiscalRegistrarTypeChanged); }
        }

        public CashboxDto FiscalRegistrarCashbox
        {
            get { return GetProperty(() => FiscalRegistrarCashbox); }
            set { SetProperty(() => FiscalRegistrarCashbox, value); }
        }

        public string FiscalRegistrarIp
        {
            get { return GetProperty(() => FiscalRegistrarIp); }
            set { SetProperty(() => FiscalRegistrarIp, value, OnFiscalRegistrarIpChanged); }
        }

        public string FiscalRegistrarMacAddress
        {
            get { return GetProperty(() => FiscalRegistrarMacAddress); }
            set { SetProperty(() => FiscalRegistrarMacAddress, value); }
        }

        public string FiscalRegistrarUser
        {
            get { return GetProperty(() => FiscalRegistrarUser); }
            set { SetProperty(() => FiscalRegistrarUser, value); }
        }

        public string FiscalRegistrarPassword
        {
            get { return GetProperty(() => FiscalRegistrarPassword); }
            set { SetProperty(() => FiscalRegistrarPassword, value); }
        }

        public string UniqueDeviceId
        {
            get { return GetProperty(() => UniqueDeviceId); }
            private set { SetProperty(() => UniqueDeviceId, value); }
        }

        public bool IsSoftwareFiscalType => FiscalRegistrarConnectTypeId == FiscalRegistrarType.SoftwareId;

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<CreateFiscalRegistrarSettingsViewModel> builder)
        {
            builder.Property(x => x.FiscalRegistrarConnectTypeId)
                .MatchesRule(x => x > 0, () => Resources.RequiredErrorMessage);

            builder.Property(x => x.FiscalRegistrarIp)
                .MatchesInstanceRule(
                    (x, y) => !string.IsNullOrEmpty(x) || y.FiscalRegistrarConnectTypeId == FiscalRegistrarType.SoftwareId,
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.FiscalRegistrarUser)
                .MatchesInstanceRule(
                    (x, y) => y.FiscalRegistrarConnectTypeId == FiscalRegistrarType.SoftwareId || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.FiscalRegistrarPassword)
                .MatchesInstanceRule(
                    (x, y) => y.FiscalRegistrarConnectTypeId == FiscalRegistrarType.SoftwareId || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.FiscalRegistrarCashbox)
                .MatchesInstanceRule(
                    (x, y) => x != null,
                    () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            FiscalRegistrarParameter parameter = Parameter as FiscalRegistrarParameter;

            UniqueDeviceId = parameter?.UniqueDeviceId;

            await Task.WhenAll(LoadCashboxesAsync(), LoadFiscalConnectionTypesAsync());

            Title = "Создание РРО";
        }

        protected override async Task HandleOkAsync()
        {
            CreateFiscalRegistrarSettingsDto createDto = new CreateFiscalRegistrarSettingsDto(
                FiscalRegistrarConnectTypeId,
                FiscalRegistrarCashbox.Id,
                null,
                FiscalRegistrarUser,
                FiscalRegistrarPassword,
                UniqueDeviceId,
                FiscalRegistrarIp,
                FiscalRegistrarMacAddress);

            Result<FiscalRegistrarSettingsDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateFiscalRegitrarSettings(createDto)),
                "создании настроек РРО",
                "Настройки РРО сохранены",
                this,
                true,
                true,
                confirmText: "Создать настройки РРО");

            if (result.IsSuccess && result.Data != null)
            {
                Messenger.Send(new FiscalRegistrarSettingsMessage(result.Data, MessageType.Added));

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

            FiscalRegistrarCashboxes = cashboxes
                .Where(x => x.IsActive && x.TypeId == CashboxType.FiscalRegistrar.Id && WebClient.AuthenticatedEmployee.AllowCashboxes.Contains(x.Id))
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadFiscalConnectionTypesAsync()
        {
            List<FiscalConnectionTypeDto> fiscalConnectionTypeDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryFiscalConnectionTypes()),
                "получении списка способов подключений",
                null,
                this,
                true,
                showNotification: false);

            FiscalRegistrarTypes = fiscalConnectionTypeDtos.ToReadOnlyObservableCollection();

            if (FiscalRegistrarTypes.Count > 0)
            {
                FiscalRegistrarConnectTypeId = FiscalRegistrarTypes.First().Id;
            }
        }

        private void OnFiscalRegistrarIpChanged()
        {
            FiscalRegistrarMacAddress = GetMac();

            RaisePropertiesChanged(nameof(FiscalRegistrarMacAddress));
        }

        private void OnFiscalRegistrarTypeChanged()
        {
            if (FiscalRegistrarConnectTypeId == FiscalRegistrarType.SoftwareId)
            {
                FiscalRegistrarIp
                    = FiscalRegistrarUser
                        = FiscalRegistrarMacAddress
                            = FiscalRegistrarPassword = string.Empty;
            }

            RaisePropertiesChanged(
                nameof(FiscalRegistrarIp),
                nameof(FiscalRegistrarUser),
                nameof(FiscalRegistrarMacAddress),
                nameof(FiscalRegistrarPassword),
                nameof(IsSoftwareFiscalType));
        }

        private string GetMac()
        {
            if (IPAddress.TryParse(FiscalRegistrarIp, out IPAddress ipAddress))
            {
                return ipAddress.GetMacByIp();
            }

            return null;
        }
    }
}