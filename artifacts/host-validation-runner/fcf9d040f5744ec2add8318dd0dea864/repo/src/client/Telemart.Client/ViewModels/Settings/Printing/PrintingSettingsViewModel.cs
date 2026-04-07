using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using Microsoft.Extensions.Logging;
using Telemart.Client.Cache;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.PosTerminal;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Settings.Equipment;
using Telemart.Client.Common.Settings.Printing;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.FiscalDocument;
using Telemart.Client.Data.Requests.Features.FiscalRegistrar;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.Requests.Features.PosTerminal;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.FiscalRegistrar.Abstraction;
using Telemart.Client.FiscalRegistrar.Requests;
using Telemart.Client.FiscalRegistrar.Responses;
using Telemart.Client.Jobs;
using Telemart.Client.PosTerminal.Ingenico;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.FiscalDocument;
using Telemart.Client.TransferObjects.FiscalRegistrar;
using Telemart.Client.TransferObjects.PosTerminal;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Settings.Printing
{
    internal sealed class PrintingSettingsViewModel : TelemartDialogViewModelBase
    {
        private readonly ITelemartClientLogger _telemartClientLogger;
        private readonly SyncCacheJob _syncCacheJob;
        private readonly ICache _cache;
        private ObservableCollection<PosSettingsItem> _allPosSettingsItems;
        private ReadOnlyObservableCollection<int> _grayLegalEntityIds;
        private int? _currentSelectedRroId;

        public PrintingSettingsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IPrintingSettingsStore settingsStore,
            IEquipmentSettingsStore equipmentSettingsStore,
            IFiscalRegistrarClientFactory fiscalRegistrarClientFactory,
            IPosTerminalFactory posTerminalFactory,
            IErrorHandler errorHandler,
            IMessenger messenger,
            SyncCacheJob syncCacheJob,
            ICache cache,
            ITelemartClientLogger telemartClientLogger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            PrintingSettingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            EquipmentSettingsStore = equipmentSettingsStore ?? throw new ArgumentNullException(nameof(equipmentSettingsStore));
            FiscalRegistrarClientFactory = fiscalRegistrarClientFactory;
            PosTerminalFactory = posTerminalFactory ?? throw new ArgumentNullException(nameof(posTerminalFactory));

            TestFiscalRegistrarCommand = new AsyncCommand(TestFiscalRegistrarAsync, CanTestFiscalRegistrar);
            TestTerminalCommand = new AsyncCommand(TestTerminalAsync, CanTestTerminal);
            SyncFiscalRegistrarCommand = new AsyncCommand(SyncFiscalRegistrarAsync, CanTestFiscalRegistrar);
            SendLogsCommand = new AsyncCommand(SendLogsAsync);
            ResetCacheCommand = new AsyncCommand(ResetCacheAsync);
            UpdateCacheCommand = new AsyncCommand(UpdateCacheAsync);
            ClearLayoutsCommand = new DelegateCommand(ClearLayouts);
            AddFiscalRegistrarSettingRroCommand = new DelegateCommand(AddFiscalRegistrarSettingRro, () => AllowEditFiscalRegistrarSettings);
            ChangeIpAddressFiscalRegistrarSettingCommand = new AsyncCommand<FiscalRegistrarRroSettingsItem>(ChangeIpAddressFiscalRegistrarSettingAsync, _ => AllowEditFiscalRegistrarSettings && SelectedRroSettingsItem?.FiscalConnectionTypeId == FiscalRegistrarType.HardwareId);
            RemoveFiscalRegistrarSettingRroCommand = new AsyncCommand(RemoveFiscalRegistrarSettingRroAsync, () => AllowEditFiscalRegistrarSettings && SelectedRroSettingsItem != null);
            AddSettingPosCommand = new DelegateCommand(AddSettingPos, () => AllowEditFiscalRegistrarSettings && SelectedRroSettingsItem != null && SelectedRroSettingsItem.FiscalRegistrarLegalEntityId.HasValue);
            ChangeIpAddressPosSettingCommand = new AsyncCommand<PosSettingsItem>(ChangeIpAddressPosSettingAsync, x => AllowEditFiscalRegistrarSettings && x != null);
            RemoveSettingPosCommand = new AsyncCommand(RemoveSettingPosAsync, () => AllowEditFiscalRegistrarSettings && SelectedPosSettingsItem != null);
            SelectFilesPathCommand = new DelegateCommand(SelectFilesPath);
            DeleteFilesPathCommand = new DelegateCommand(DeleteFilesPath);

            AllowEditEquipmentSettings = WebClient.IsOperationAllowed(BusinessOperation.EquipmentSettingsEditAccess);
            AllowEditFiscalRegistrarSettings = AllowEditEquipmentSettings;

            ErrorHandler = errorHandler;
            Messenger = messenger;
            _syncCacheJob = syncCacheJob;
            _cache = cache;
            _telemartClientLogger = telemartClientLogger;

            Messenger.Register<PosSettingsMessage>(this, OnPosSettingsMessage);
            Messenger.Register<FiscalRegistrarSettingsMessage>(this, OnFiscalRegistrarMessage);
        }

        public PrintingSettingsViewModel()
        {
        }

        #region Commands

        public IAsyncCommand TestFiscalRegistrarCommand { get; }

        public IAsyncCommand TestTerminalCommand { get; }

        public IAsyncCommand SyncFiscalRegistrarCommand { get; }

        public IAsyncCommand SendLogsCommand { get; }

        public IAsyncCommand ResetCacheCommand { get; }

        public IAsyncCommand UpdateCacheCommand { get; }

        public IDelegateCommand ClearLayoutsCommand { get; }

        public IDelegateCommand AddFiscalRegistrarSettingRroCommand { get; }

        public IAsyncCommand ChangeIpAddressFiscalRegistrarSettingCommand { get; }

        public IAsyncCommand RemoveFiscalRegistrarSettingRroCommand { get; }

        public IDelegateCommand AddSettingPosCommand { get; }

        public IAsyncCommand ChangeIpAddressPosSettingCommand { get; }

        public IAsyncCommand RemoveSettingPosCommand { get; }

        public IDelegateCommand SelectFilesPathCommand { get; }

        public IDelegateCommand DeleteFilesPathCommand { get; }

        #endregion

        public ReadOnlyObservableCollection<CashboxDto> FiscalRegistrarCashboxes
        {
            get { return GetProperty(() => FiscalRegistrarCashboxes); }
            private set { SetProperty(() => FiscalRegistrarCashboxes, value); }
        }

        public ReadOnlyObservableCollection<CashboxDto> PosCashboxes
        {
            get { return GetProperty(() => PosCashboxes); }
            private set { SetProperty(() => PosCashboxes, value); }
        }

        public ReadOnlyObservableCollection<PrinterInfo> Printers
        {
            get { return GetProperty(() => Printers); }
            private set { SetProperty(() => Printers, value); }
        }

        public ReadOnlyObservableCollection<PrintingSettingsChequeFormat> ChequeFormats
        {
            get { return GetProperty(() => ChequeFormats); }
            private set { SetProperty(() => ChequeFormats, value); }
        }

        public ReadOnlyObservableCollection<PrintingSettingsInvoiceFormat> InvoiceFormats
        {
            get { return GetProperty(() => InvoiceFormats); }
            private set { SetProperty(() => InvoiceFormats, value); }
        }

        public ReadOnlyObservableCollection<PrintingSettingsBarcodeFormat> BarcodeFormats
        {
            get { return GetProperty(() => BarcodeFormats); }
            private set { SetProperty(() => BarcodeFormats, value); }
        }

        public ReadOnlyObservableCollection<PrintingSettingsWarrantyFormat> WarrantyFormats
        {
            get { return GetProperty(() => WarrantyFormats); }
            private set { SetProperty(() => WarrantyFormats, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> LegalEntities
        {
            get { return GetProperty(() => LegalEntities); }
            private set { SetProperty(() => LegalEntities, value); }
        }

        public ReadOnlyObservableCollection<FiscalConnectionTypeDto> FiscalRegistrarTypes
        {
            get { return GetProperty(() => FiscalRegistrarTypes); }
            set { SetProperty(() => FiscalRegistrarTypes, value); }
        }

        public ReadOnlyObservableCollection<PosTerminalType> PosTypes
        {
            get { return GetProperty(() => PosTypes); }
            set { SetProperty(() => PosTypes, value); }
        }

        public ObservableCollection<FiscalRegistrarRroSettingsItem> FiscalRegistrarSettingRroItems
        {
            get { return GetProperty(() => FiscalRegistrarSettingRroItems); }
            set { SetProperty(() => FiscalRegistrarSettingRroItems, value); }
        }

        public ObservableCollection<PosSettingsItem> SettingPosItems
        {
            get { return GetProperty(() => SettingPosItems); }
            set { SetProperty(() => SettingPosItems, value); }
        }

        public FiscalRegistrarRroSettingsItem SelectedRroSettingsItem
        {
            get { return GetProperty(() => SelectedRroSettingsItem); }
            set { SetProperty(() => SelectedRroSettingsItem, value, ChangedSelectedFiscalRegistrarRro); }
        }

        public PosSettingsItem SelectedPosSettingsItem
        {
            get { return GetProperty(() => SelectedPosSettingsItem); }
            set { SetProperty(() => SelectedPosSettingsItem, value); }
        }

        public int SelectedChequeFormat
        {
            get { return GetProperty(() => SelectedChequeFormat); }
            set { SetProperty(() => SelectedChequeFormat, value, ChequeFormatChanged); }
        }

        public int SelectedInvoiceFormat
        {
            get { return GetProperty(() => SelectedInvoiceFormat); }
            set { SetProperty(() => SelectedInvoiceFormat, value, InvoiceFormatChanged); }
        }

        public int SelectedBarcodeFormat
        {
            get { return GetProperty(() => SelectedBarcodeFormat); }
            set { SetProperty(() => SelectedBarcodeFormat, value, BarcodeFormatChanged); }
        }

        public int SelectedWarrantyFormat
        {
            get { return GetProperty(() => SelectedWarrantyFormat); }
            set { SetProperty(() => SelectedWarrantyFormat, value); }
        }

        public bool? AllowPrintBarcodeAssemblyProduct
        {
            get { return GetProperty(() => AllowPrintBarcodeAssemblyProduct); }
            set { SetProperty(() => AllowPrintBarcodeAssemblyProduct, value); }
        }

        public bool? AllowShowChoosePrintRroCheck
        {
            get { return GetProperty(() => AllowShowChoosePrintRroCheck); }
            set { SetProperty(() => AllowShowChoosePrintRroCheck, value); }
        }

        public ReadOnlyObservableCollection<PrintingSettingsViewItem> PrintingSettings
        {
            get { return GetProperty(() => PrintingSettings); }
            private set { SetProperty(() => PrintingSettings, value); }
        }

        public TimeSpan? LockTimeout
        {
            get { return GetProperty(() => LockTimeout); }
            set { SetProperty(() => LockTimeout, value, OnLockTimeoutChanged); }
        }

        public bool AllowEditEquipmentSettings
        {
            get { return GetProperty(() => AllowEditEquipmentSettings); }
            private init { SetProperty(() => AllowEditEquipmentSettings, value); }
        }

        public bool AllowEditFiscalRegistrarSettings
        {
            get { return GetProperty(() => AllowEditFiscalRegistrarSettings); }
            private set { SetProperty(() => AllowEditFiscalRegistrarSettings, value); }
        }

        public Guid UniqueDeviceId
        {
            get { return GetProperty(() => UniqueDeviceId); }
            set { SetProperty(() => UniqueDeviceId, value); }
        }

        public string FilesPath
        {
            get { return GetProperty(() => FilesPath); }
            set { SetProperty(() => FilesPath, value); }
        }

        public string ThemeName
        {
            get { return GetProperty(() => ThemeName); }
            set { SetProperty(() => ThemeName, value); }
        }

        public ComboBoxItem[] Themes
        {
            get { return GetProperty(() => Themes); }
            private set { SetProperty(() => Themes, value); }
        }

        private IPrintingSettingsStore PrintingSettingsStore { get; }

        private IEquipmentSettingsStore EquipmentSettingsStore { get; }

        private IFiscalRegistrarClientFactory FiscalRegistrarClientFactory { get; }

        private IPosTerminalFactory PosTerminalFactory { get; }

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        private IFolderBrowserDialogService FolderBrowserDialogService => GetService<IFolderBrowserDialogService>();

        public static void BuildMetadata(MetadataBuilder<PrintingSettingsViewModel> builder)
        {
            builder.Property(x => x.LockTimeout)
                .MatchesRule(
                    x => x is null || x >= TimeSpan.Zero,
                    () => "Значение не может быть отрицательными")
                .MatchesRule(
                    x => x is null || x <= TimeSpan.FromHours(24),
                    () => "Значение не может быть больше 24 часов");
        }

        protected override async Task HandleLoadedAsync()
        {
            ReadOnlyObservableCollection<PrinterInfo> printers = await GetInstalledPrintersAsync();

            PrintingSettingsInfo printingSettings = await PrintingSettingsStore.LoadAsync();

            EquipmentSettingsInfo equipmentSettings = await EquipmentSettingsStore.LoadAsync();

            UniqueDeviceId = equipmentSettings.UniqueDeviceGuid!.Value;
            LockTimeout = equipmentSettings.LockTimeout;
            ThemeName = equipmentSettings.ThemeName;

            PosTypes = Dictionaries.GetItems<PosTerminalType>().ToReadOnlyObservableCollection();
            Themes = new []
            {
                new ComboBoxItem(1, "Системная", reference: Theme.Win10SystemName),
                new ComboBoxItem(2, "Темная", reference: Theme.Win10DarkName),
                new ComboBoxItem(3, "Светлая", reference: Theme.Win10LightName),
                new ComboBoxItem(4, "Титановая", reference: Theme.Office2016DarkGraySEName),
                new ComboBoxItem(5, "Графитовая", reference: Theme.Office2016BlackSEName)
            };

            await Task.WhenAll(
                LoadLegalEntitiesAsync(),
                LoadCashboxesAsync(),
                LoadFiscalRegistrarSettingsAsync(),
                LoadFiscalConnectionTypesAsync(),
                LoadPosSettingsAsync());

            SelectedBarcodeFormat = printingSettings.BarcodeFormat!.Value;
            SelectedInvoiceFormat = printingSettings.InvoiceFormat!.Value;
            SelectedChequeFormat = printingSettings.ChequeFormat!.Value;
            SelectedWarrantyFormat = printingSettings.WarrantyFormat ?? PrintingSettingsWarrantyFormat.A5.Id;
            AllowPrintBarcodeAssemblyProduct = printingSettings.PrintBarcodeAssemblyProduct;
            AllowShowChoosePrintRroCheck = printingSettings.ShowChoosePrintRroCheck;
            FilesPath = printingSettings.FilesPath;

            ChequeFormats = Dictionaries
                .GetItems<PrintingSettingsChequeFormat>()
                .Where(x => x.InvoiceFormatId == SelectedInvoiceFormat || x.InvoiceFormatId == null)
                .ToReadOnlyObservableCollection();

            InvoiceFormats = Dictionaries.GetItems<PrintingSettingsInvoiceFormat>().ToReadOnlyObservableCollection();
            BarcodeFormats = Dictionaries.GetItems<PrintingSettingsBarcodeFormat>().ToReadOnlyObservableCollection();
            WarrantyFormats = Dictionaries.GetItems<PrintingSettingsWarrantyFormat>().ToReadOnlyObservableCollection();
            Printers = printers;
            PrintingSettings = GetPrintingSettings(printingSettings).OrderBy(x => x.Type.Position).ToReadOnlyObservableCollection();

            Title = "Настройки";
        }

        protected override async Task HandleOkAsync()
        {
            if (PrintingSettings.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                Logger.LogInformation("{V}", string.Join("\n", PrintingSettings.Select(x => $"{x.Type.Name} => {string.Join(", ", GetAllErrors(x))}")));
                return;
            }

            PrintingSettingsInfo printingSettings = new PrintingSettingsInfo(
                Map(PrintingSettings.First(x => x.Type == PrintingSettingsType.Main)),
                Map(PrintingSettings.First(x => x.Type == PrintingSettingsType.Cheque)),
                Map(PrintingSettings.First(x => x.Type == PrintingSettingsType.Sticker)),
                Map(PrintingSettings.First(x => x.Type == PrintingSettingsType.WarrantyCard)),
                Map(PrintingSettings.First(x => x.Type == PrintingSettingsType.Barcode30X20)),
                Map(PrintingSettings.First(x => x.Type == PrintingSettingsType.Barcode50X40)),
                Map(PrintingSettings.First(x => x.Type == PrintingSettingsType.SerialNumber)),
                SelectedChequeFormat,
                SelectedBarcodeFormat,
                SelectedInvoiceFormat,
                SelectedWarrantyFormat,
                AllowPrintBarcodeAssemblyProduct ?? false,
                AllowShowChoosePrintRroCheck ?? true,
                FilesPath);

            await PrintingSettingsStore.SaveAsync(printingSettings);

            var equipmentSettings = await EquipmentSettingsStore.LoadAsync();

            if (equipmentSettings.ThemeName != ThemeName)
            {
                equipmentSettings.ThemeName = ThemeName;
                ApplicationThemeHelper.ApplicationThemeName = equipmentSettings.ThemeName;

                await EquipmentSettingsStore.SaveAsync(equipmentSettings);
            }

            await SaveSelectedFiscalRegistrarSettingRroAsync();

            MessageFacadeService.ShowNotificationInfo("Настройки успешно сохранены");

            IsOk = true;
            Close();
        }

        private static string[] GetAllErrors(IDataErrorInfo item)
        {
            return TypeDescriptor.GetProperties(item)
                .Cast<PropertyDescriptor>()
                .Select(p => IDataErrorInfoHelper.GetErrorText(item, p.Name)).Where(msg => !string.IsNullOrWhiteSpace(msg))
                .ToArray();
        }

        private static PrinterSettingsInfo Map(PrintingSettingsViewItem item)
        {
            return new PrinterSettingsInfo(item.Printer?.Name, item.PaperSource);
        }

        private Task<ReadOnlyObservableCollection<PrinterInfo>> GetInstalledPrintersAsync()
        {
            return Task<ReadOnlyObservableCollection<PrinterInfo>>.Factory.StartNew(
                () =>
            {
                try
                {
                    IEnumerable<PrinterInfo> query = from printerName in PrinterSettings.InstalledPrinters.Cast<string>()
                                                     let printerSettings = new PrinterSettings { PrinterName = printerName }
                                                     where printerSettings.IsValid
                                                     let paperSources = printerSettings.PaperSources.Cast<PaperSource>().Select(paperSource => paperSource.SourceName)
                                                     select new PrinterInfo(printerName, paperSources.ToArray());

                    return query.OrderBy(x => x.Name).ToReadOnlyObservableCollection();
                }
                catch (Exception e)
                {
                    MessageFacadeService.ShowNotificationWarning("Ошибка при запросе системных принтеров");

                    Logger.LogError(e, "Failed to get installed printers");
                    return Array.Empty<PrinterInfo>().ToReadOnlyObservableCollection();
                }
            },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task SaveSelectedFiscalRegistrarSettingRroAsync()
        {
            int? selectedRroId = FiscalRegistrarSettingRroItems.FirstOrDefault(x => x.Selected)?.Id;

            if (selectedRroId.HasValue && selectedRroId != _currentSelectedRroId)
            {
                await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new SelectFiscalRegistrarSettings(selectedRroId.Value, UniqueDeviceId.ToString())),
                    "сохранении выбранного фискального регистратора",
                    null,
                    this,
                    true,
                    showNotification: false,
                    showError: false);
            }
        }

        private async Task ResetCacheAsync()
        {
            await _cache.DeleteAllAsync();

            await UpdateCacheAsync();
        }

        private async Task UpdateCacheAsync()
        {
            SplashScreenManager splashScreenManager = SplashScreenManager.CreateWaitIndicator();
            splashScreenManager.Show();

            await _syncCacheJob.ExecuteOnceAsync(default);
            await Dictionaries.LoadAsync();

            splashScreenManager.Close();
        }

        private async Task SendLogsAsync()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter("Кратко опишите проблему", "Описание проблемы", isMultiline: true);

            GetTextFromUserViewModel viewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            await _telemartClientLogger.SendLogsAsync(viewModel.Content);
        }

        private void ClearLayouts()
        {
            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>(
                "Удаление необратимо. Вы уверены?",
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                DirectoryInfo layoutsDirInfo = new DirectoryInfo($"{appDataPath}\\telemart.client\\layouts");

                foreach (FileInfo file in layoutsDirInfo.GetFiles())
                {
                    file.Delete();
                }

                DirectoryInfo settingsDirInfo = new DirectoryInfo($"{appDataPath}\\telemart.client\\settings");

                foreach (FileInfo file in settingsDirInfo.GetFiles())
                {
                    file.Delete();
                }

                MessageFacadeService.ShowNotificationInfo("Макеты успешно очищены");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while clearing layouts");
                MessageFacadeService.ShowNotificationError("Ошибка при очистке макетов");
            }
        }

        private async Task TestTerminalAsync()
        {
            if (SelectedPosSettingsItem != null)
            {
                try
                {
                    PosType type = (PosType)SelectedPosSettingsItem.PosTypeId;

                    IPosTerminalClient posTerminalClient = PosTerminalFactory.Create(type, SelectedPosSettingsItem.IpAddress);

                    bool isConnected = await Task.Factory.StartNew(() => posTerminalClient.IsConnected());

                    if (isConnected)
                    {
                        MessageFacadeService.ShowNotificationInfo("Связь с терминалом установлена");
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationWarning("Ошибка связи с терминалом");
                    }
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to test POS terminal");

                    if (exception.Message.Contains("Retrieving the COM class factory for component with CLSID"))
                    {
                        if (MessageFacadeService.Confirm("На данном устройстве не установлен Com объект для работы с терминалами. Установить необходимое ПО?", "Ошибка при тестировании терминала"))
                        {
                            await WebClient.InstallationPosComObjectAsync();
                        }
                    }
                }
            }
        }

        private IEnumerable<PrintingSettingsViewItem> GetPrintingSettings(PrintingSettingsInfo settings)
        {
            yield return Map(settings, PrintingSettingsType.Main, x => x.Main);
            yield return Map(settings, PrintingSettingsType.Cheque, x => x.Cheque);
            yield return Map(settings, PrintingSettingsType.WarrantyCard, x => x.WarrantyCard);
            yield return Map(settings, PrintingSettingsType.Sticker, x => x.Sticker);
            yield return Map(settings, PrintingSettingsType.Barcode30X20, x => x.Barcode);
            yield return Map(settings, PrintingSettingsType.Barcode50X40, x => x.Barcode50X40);
            yield return Map(settings, PrintingSettingsType.SerialNumber, x => x.SerialNumber);
        }

        private PrintingSettingsViewItem Map(
            PrintingSettingsInfo printingSettings,
            PrintingSettingsType type,
            Func<PrintingSettingsInfo, PrinterSettingsInfo> getSettings)
        {
            PrintingSettingsViewItem item = new PrintingSettingsViewItem
            {
                Type = type,
                Required = Printers?.Any() == true && type == PrintingSettingsType.Main
            };

            if (printingSettings != null)
            {
                PrinterSettingsInfo printerSettings = getSettings(printingSettings);

                PrinterInfo printer = Printers.FirstOrDefault(x => x.Name == printerSettings?.Name);

                if (printer != null)
                {
                    item.Printer = printer;
                    item.PaperSource = printerSettings.PaperSource;
                }
            }

            return item;
        }

        private void ChequeFormatChanged()
        {
            if (PrintingSettings != null)
            {
                PrintingSettingsViewItem cheque = PrintingSettings.First(x => x.Type == PrintingSettingsType.Cheque);
                cheque.Required = Printers?.Any() == true && SelectedChequeFormat == PrintingSettingsChequeFormat.CheckTape.Id;
            }
        }

        private void BarcodeFormatChanged()
        {
            if (PrintingSettings != null)
            {
                PrintingSettingsViewItem barcode30X20 = PrintingSettings.First(x => x.Type == PrintingSettingsType.Barcode30X20);
                barcode30X20.Required = Printers?.Any() == true && SelectedBarcodeFormat == PrintingSettingsBarcodeFormat.Barcode30X20.Id;

                PrintingSettingsViewItem barcode50X40 = PrintingSettings.First(x => x.Type == PrintingSettingsType.Barcode50X40);
                barcode50X40.Required = Printers?.Any() == true && SelectedBarcodeFormat == PrintingSettingsBarcodeFormat.Barcode50X40.Id;
            }
        }

        private void InvoiceFormatChanged()
        {
            if (SelectedChequeFormat != PrintingSettingsChequeFormat.CheckTape.Id)
            {
                SelectedChequeFormat = 0;
            }

            ChequeFormats = Dictionaries
                .GetItems<PrintingSettingsChequeFormat>()
                .Where(x => x.InvoiceFormatId == SelectedInvoiceFormat || x.Id == PrintingSettingsChequeFormat.CheckTape.Id)
                .ToReadOnlyObservableCollection();
        }

        private bool CanTestFiscalRegistrar()
        {
            if (SelectedRroSettingsItem != null)
            {
                return !string.IsNullOrWhiteSpace(SelectedRroSettingsItem.FiscalRegistrarIp)
                       && !string.IsNullOrWhiteSpace(SelectedRroSettingsItem.FiscalRegistrarUser)
                       && !string.IsNullOrWhiteSpace(SelectedRroSettingsItem.FiscalRegistrarPassword);
            }

            return false;
        }

        private bool CanTestTerminal()
        {
            if (SelectedPosSettingsItem != null)
            {
                return !string.IsNullOrWhiteSpace(SelectedPosSettingsItem.IpAddress)
                       && !string.IsNullOrWhiteSpace(SelectedPosSettingsItem.Merchant);
            }

            return false;
        }

        private async Task TestFiscalRegistrarAsync()
        {
            if (SelectedRroSettingsItem == null)
            {
                return;
            }

            Type type = SelectedRroSettingsItem.FiscalConnectionTypeId == FiscalRegistrarType.HardwareId
                ? FiscalRegistrarType.Hardware.Type
                : FiscalRegistrarType.Software.Type;

            IFiscalRegistrarClient fiscalRegistrarClient = await FiscalRegistrarClientFactory.CreateAsync(
                type,
                MapFiscalSettings(),
                CancellationToken.None);

            try
            {
                CashboxDto cashbox = FiscalRegistrarCashboxes.FirstOrDefault(x => x.Id == SelectedRroSettingsItem.FiscalRegistrarCashboxId);

                if (cashbox != null)
                {
                    bool getStateResult = await ErrorHandler.HandleErrorsAsync(
                        ct => fiscalRegistrarClient.GetStateAsync(cashbox.Id, ct),
                        "проверке статуса РРО",
                        "Проверка статуса РРО прошла",
                        this,
                        true);

                    if (getStateResult)
                    {
                        MessageFacadeService.ShowMessageBoxInfo("Устройство готово к работе");
                    }
                }
            }
            catch
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private FiscalRegistrarSettingsDto MapFiscalSettings()
        {
            return new FiscalRegistrarSettingsDto
            {
                CashboxId = SelectedRroSettingsItem.FiscalRegistrarCashboxId,
                Active = SelectedRroSettingsItem.Active,
                FiscalConnectionTypeId = SelectedRroSettingsItem.FiscalConnectionTypeId,
                Id = SelectedRroSettingsItem.Id,
                IpAddress = SelectedRroSettingsItem.FiscalRegistrarIp,
                Login = SelectedRroSettingsItem.FiscalRegistrarUser,
                Selected = SelectedRroSettingsItem.Selected,
                Password = SelectedRroSettingsItem.FiscalRegistrarPassword,
                MacAddress = SelectedRroSettingsItem.MacAddress,
                UniqueDeviceId = SelectedRroSettingsItem.UniqueDeviceId,
                SessionIsOpen = SelectedRroSettingsItem.SessionIsOpen,
                LegalEntityId = SelectedRroSettingsItem.FiscalRegistrarLegalEntityId
            };
        }

        private async Task SyncFiscalRegistrarAsync()
        {
            if (SelectedRroSettingsItem == null)
            {
                return;
            }

            Type type = SelectedRroSettingsItem.FiscalConnectionTypeId == FiscalRegistrarType.HardwareId
                ? FiscalRegistrarType.Hardware.Type
                : FiscalRegistrarType.Software.Type;

            IFiscalRegistrarClient fiscalRegistrarClient = await FiscalRegistrarClientFactory.CreateAsync(
                type,
                MapFiscalSettings(),
                CancellationToken.None);

            if (!(fiscalRegistrarClient is FiscalRegistrarClient client))
            {
                MessageFacadeService.ShowNotificationWarning("Синхронизацию поддерживает только аппаратный РРО");
                return;
            }

            try
            {
                List<FiscalDocumentPaymentTypeDto> paymentTypes = await WebClient.ExecuteApiRequestAsync(new QueryFiscalDocumentPaymentTypes());

                IEnumerable<PaymentItem> paymentItems = paymentTypes.Select(x => new PaymentItem(x.Id, x.Name));

                EditTableResponse editPaymentTableResponse = await client.EditPaymentTableAsync(new EditPaymentTableRequest(paymentItems), CancellationToken.None);

                if (!editPaymentTableResponse.IsOk)
                {
                    ShowValidationResultView(
                        "Ошибки",
                        editPaymentTableResponse.GetErrorMessages().Select(x => new ValidationResultItem(x, true)).ToArray());
                    return;
                }

                MessageFacadeService.ShowMessageBoxInfo("Таблицы успешно синхронизированы");
            }
            catch (Exception ex)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(ex, Resources.ErrorDuringDataLoading);
            }
        }

        private void OnLockTimeoutChanged()
        {
            if (Loaded)
            {
                Messenger.Send(new LockTimeoutChangedMessage(LockTimeout));
            }
        }

        private async Task LoadLegalEntitiesAsync()
        {
            List<LegalEntityDto> legalEntities = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryLegalEntities(), true),
                "получении списка юр. лиц",
                null,
                this,
                true,
                showNotification: false);

            if (legalEntities != null)
            {
                LegalEntities = legalEntities
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();

                _grayLegalEntityIds = legalEntities
                    .Where(x => x.White == false)
                    .Select(x => x.Id)
                    .ToReadOnlyObservableCollection();
            }
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

            if (cashboxes != null)
            {
                FiscalRegistrarCashboxes = cashboxes
                    .Where(x => x.IsActive && x.TypeId == CashboxType.FiscalRegistrar.Id && WebClient.AuthenticatedEmployee.AllowCashboxes.Contains(x.Id))
                    .ToReadOnlyObservableCollection();

                PosCashboxes = cashboxes
                    .Where(x => x.IsActive && x.AllowedPayments.Contains(Payment.TerminalId) && WebClient.AuthenticatedEmployee.AllowCashboxes.Contains(x.Id))
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task LoadFiscalRegistrarSettingsAsync()
        {
            List<FiscalRegistrarSettingsDto> fiscalRegistrarSettingsDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryFiscalRegistrarSettings(UniqueDeviceId.ToString())),
                "получении списка настроек РРО по уникальному id компа",
                null,
                this,
                true,
                showNotification: false);

            if (fiscalRegistrarSettingsDtos != null)
            {
                FiscalRegistrarSettingRroItems = fiscalRegistrarSettingsDtos
                    .Where(x => x.Active)
                    .Select(MapFiscalRegistrarRroSettingsItem)
                    .ToObservableCollection();

                _currentSelectedRroId = fiscalRegistrarSettingsDtos.FirstOrDefault(x => x.Selected)?.Id;
            }
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

            if (fiscalConnectionTypeDtos != null)
            {
                FiscalRegistrarTypes = fiscalConnectionTypeDtos.ToReadOnlyObservableCollection();
            }
        }

        private async Task LoadPosSettingsAsync()
        {
            List<PosSettingsDto> posSettingsDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryPosSettings(UniqueDeviceId.ToString())),
                "получении списка настроек пос-терминалов",
                null,
                this,
                true,
                showNotification: false);

            _allPosSettingsItems = posSettingsDtos.Where(x => x.Active)
                .Select(MapPosSettingsItem)
                .ToObservableCollection();
        }

        private FiscalRegistrarRroSettingsItem MapFiscalRegistrarRroSettingsItem(FiscalRegistrarSettingsDto source)
        {
            FiscalRegistrarRroSettingsItem item = new FiscalRegistrarRroSettingsItem();

            item.Id = source.Id;
            item.FiscalConnectionTypeId = source.FiscalConnectionTypeId;
            item.FiscalRegistrarCashboxId = source.CashboxId;
            item.FiscalRegistrarLegalEntityId = source.LegalEntityId;
            item.FiscalRegistrarIp = source.IpAddress;
            item.FiscalRegistrarUser = source.Login;
            item.FiscalRegistrarPassword = source.Password;
            item.Selected = source.Selected;
            item.Active = source.Active;
            item.UniqueDeviceId = source.UniqueDeviceId;
            item.MacAddress = source.MacAddress;
            item.SessionIsOpen = source.SessionIsOpen;

            return item;
        }

        private PosSettingsItem MapPosSettingsItem(PosSettingsDto source)
        {
            PosSettingsItem item = new PosSettingsItem();

            item.Id = source.Id;
            item.Merchant = source.Merchant;
            item.IpAddress = source.IpAddress;
            item.PosCashboxId = source.CashboxId;
            item.PosTypeId = source.PosTypeId;
            item.PosLegalEntityId = source.LegalEntityId;
            item.MacAddress = source.MacAddress;

            return item;
        }

        private void ChangedSelectedFiscalRegistrarRro()
        {
            if (SelectedRroSettingsItem != null)
            {
                SettingPosItems = _allPosSettingsItems
                    .Where(x =>
                        (SelectedRroSettingsItem.FiscalRegistrarLegalEntityId.HasValue
                         && x.PosLegalEntityId == SelectedRroSettingsItem.FiscalRegistrarLegalEntityId.Value)
                            || !x.PosLegalEntityId.HasValue
                            || _grayLegalEntityIds.Contains(x.PosLegalEntityId.Value))
                    .ToObservableCollection();
            }
            else
            {
                SettingPosItems = null;
            }
        }

        private void AddFiscalRegistrarSettingRro()
        {
            FiscalRegistrarParameter parameter = new FiscalRegistrarParameter(UniqueDeviceId.ToString());

            DialogDocumentManagerService.ShowView<CreateFiscalRegistrarSettingsViewModel>(parameter, this);
        }

        private async Task ChangeIpAddressFiscalRegistrarSettingAsync(FiscalRegistrarRroSettingsItem item)
        {
            if (item == null)
            {
                return;
            }

            EditIpAddressParameter parameter = new EditIpAddressParameter(item.FiscalRegistrarIp, item.MacAddress);

            EditIpAddressViewModel model = DialogDocumentManagerService.ShowView<EditIpAddressViewModel>(parameter, this);

            if (model.IsOk)
            {
                ChangeIpAddressSettingDto changeIpAddressPosSetting = new ChangeIpAddressSettingDto(model.IpAddress, model.MacAddress);

                Result<FiscalRegistrarSettingsDto> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new ChangeFiscalRegistrarIpAddress(item.Id, changeIpAddressPosSetting)),
                    "изменения IP-адрес РРО",
                    "IP-адрес РРО изменен",
                    this,
                    true,
                    showNotification: true);

                if (result.IsSuccess)
                {
                    SelectedRroSettingsItem.FiscalRegistrarIp = result.Data?.IpAddress;
                }
            }
        }

        private async Task RemoveFiscalRegistrarSettingRroAsync()
        {
            if (SelectedRroSettingsItem != null)
            {
                if (FiscalRegistrarSettingRroItems.Count > 1 && SelectedRroSettingsItem.Selected)
                {
                    MessageFacadeService.ShowMessageBoxError("Нельзя удалять активную запись.");

                    return;
                }

                if (MessageFacadeService.Confirm("Вы уверены?", "Удаление настроек РРО"))
                {
                    await SaveSelectedFiscalRegistrarSettingRroAsync();

                    Result result = await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new DeactivateFiscalRegistrarSettings(SelectedRroSettingsItem.Id)),
                        "удалении пос-терминала",
                        null,
                        this,
                        true,
                        showNotification: false);

                    if (result.IsSuccess)
                    {
                        FiscalRegistrarSettingRroItems.Remove(SelectedRroSettingsItem);
                    }
                }
            }
        }

        private void AddSettingPos()
        {
            PosParameter parameter = new PosParameter(SelectedRroSettingsItem.FiscalRegistrarLegalEntityId, UniqueDeviceId.ToString());

            DialogDocumentManagerService.ShowView<CreatePosSettingsViewModel>(parameter, this);
        }

        private async Task ChangeIpAddressPosSettingAsync(PosSettingsItem item)
        {
            if (item == null)
            {
                return;
            }

            EditIpAddressParameter parameter = new EditIpAddressParameter(item.IpAddress, item.MacAddress);

            EditIpAddressViewModel model = DialogDocumentManagerService.ShowView<EditIpAddressViewModel>(parameter, this);

            if (model.IsOk)
            {
                ChangeIpAddressSettingDto changeIpAddressPosSetting = new ChangeIpAddressSettingDto(string.IsNullOrEmpty(model.Port) ? model.IpAddress : $"{model.IpAddress}:{model.Port}", model.MacAddress);

                Result<PosSettingsDto> result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new ChangePosSettingIpAddress(item.Id, changeIpAddressPosSetting)),
                    "изменения IP-адрес терминала",
                    "IP-адрес терминала изменен",
                    this,
                    true,
                    showNotification: true);

                if (result.IsSuccess)
                {
                    PosSettingsItem posSettingsItem = _allPosSettingsItems.FirstOrDefault(x => x.Id == item.Id);

                    if (posSettingsItem != null)
                    {
                        posSettingsItem.IpAddress = result.Data?.IpAddress;
                        ChangedSelectedFiscalRegistrarRro();
                    }
                }
            }
        }

        private async Task RemoveSettingPosAsync()
        {
            if (SelectedPosSettingsItem != null)
            {
                Result result = await ErrorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new DeactivatePosSettings(SelectedPosSettingsItem.Id)),
                    "удалении пос-терминала",
                    null,
                    this,
                    true,
                    showNotification: false);

                if (result.IsSuccess)
                {
                    _allPosSettingsItems.Remove(SelectedPosSettingsItem);

                    ChangedSelectedFiscalRegistrarRro();
                }
            }
        }

        private void OnPosSettingsMessage(PosSettingsMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                {
                    PosSettingsItem item = MapPosSettingsItem(message.Entity);

                    _allPosSettingsItems.Add(item);

                    ChangedSelectedFiscalRegistrarRro();
                }

                break;
            }
        }

        private void OnFiscalRegistrarMessage(FiscalRegistrarSettingsMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                {
                    FiscalRegistrarRroSettingsItem item = MapFiscalRegistrarRroSettingsItem(message.Entity);

                    foreach (FiscalRegistrarRroSettingsItem rroItem in FiscalRegistrarSettingRroItems)
                    {
                        rroItem.Selected = false;
                    }

                    FiscalRegistrarSettingRroItems.Add(item);

                    SelectedRroSettingsItem = item;
                }

                break;
            }
        }

        private void SelectFilesPath()
        {
            bool isOk = FolderBrowserDialogService.ShowDialog();

            if (isOk)
            {
                FilesPath = FolderBrowserDialogService.ResultPath;
            }
        }

        private void DeleteFilesPath()
        {
            FilesPath = string.Empty;
        }
    }
}