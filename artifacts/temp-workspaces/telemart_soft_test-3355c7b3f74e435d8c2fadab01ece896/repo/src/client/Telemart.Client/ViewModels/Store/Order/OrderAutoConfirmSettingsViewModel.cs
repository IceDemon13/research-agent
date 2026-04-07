using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DynamicData;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderAutoConfirmSettingsViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;

        private TelemartEnumerableCompareHelper<OrderAutoConfirmSettingViewItem> _compareHelper;

        public OrderAutoConfirmSettingsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            _errorHandler = errorHandler;

            DeleteCommand = new DelegateCommand(Delete, () => SelectedSetting != null);
            AddCommand = new DelegateCommand(Add);
            HandleToActiveChangedCommand = new DelegateCommand(SalesHistoryDeviationChanged);
        }

        public IDelegateCommand DeleteCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand HandleToActiveChangedCommand { get; }

        public ReadOnlyObservableCollection<Payment> Payments
        {
            get { return GetProperty(() => Payments); }
            set { SetProperty(() => Payments, value); }
        }

        public ReadOnlyObservableCollection<CarryType> Carries
        {
            get { return GetProperty(() => Carries); }
            set { SetProperty(() => Carries, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> OrderProductSources
        {
            get { return GetProperty(() => OrderProductSources); }
            private set { SetProperty(() => OrderProductSources, value); }
        }

        public ObservableCollection<OrderAutoConfirmSettingViewItem> Settings
        {
            get { return GetProperty(() => Settings); }
            set { SetProperty(() => Settings, value); }
        }

        public OrderAutoConfirmSettingViewItem SelectedSetting
        {
            get { return GetProperty(() => SelectedSetting); }
            set { SetProperty(() => SelectedSetting, value); }
        }

        public OrderGeneralConfirmSettingViewItem SalesHistoryDays
        {
            get { return GetProperty(() => SalesHistoryDays); }
            private set { SetProperty(() => SalesHistoryDays, value); }
        }

        public OrderGeneralConfirmSettingViewItem SalesHistoryDeviation
        {
            get { return GetProperty(() => SalesHistoryDeviation); }
            private set { SetProperty(() => SalesHistoryDeviation, value); }
        }

        public OrderGeneralConfirmSettingViewItem AllowSalesHistoryDeviation
        {
            get { return GetProperty(() => AllowSalesHistoryDeviation); }
            private set { SetProperty(() => AllowSalesHistoryDeviation, value, SalesHistoryDeviationChanged); }
        }

        public OrderGeneralConfirmSettingViewItem AllowableOnlyDontCall
        {

            get { return GetProperty(() => AllowableOnlyDontCall); }
            private set { SetProperty(() => AllowableOnlyDontCall, value); }
        }

        public override void OnClose(CancelEventArgs e)
        {
            if (!IsOk && _compareHelper?.IsChanged() == true && !MessageFacadeService.Confirm("Закрыть диалог без сохранения изменений?"))
            {
                e.Cancel = true;
            }
            else
            {
                base.OnClose(e);
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            Payments = Dictionaries.GetItems<Payment>().ToReadOnlyObservableCollection();
            Carries = Dictionaries.GetItems<CarryType>().ToReadOnlyObservableCollection();
            OrderProductSources = Dictionaries
                .GetItems<OrderProductSourceType>()
                .Where(x => x.Id == OrderProductSourceType.MovementId || x.Id == OrderProductSourceType.PurchaseId || x.Id == OrderProductSourceType.WarehouseSourceId || x.Id == OrderProductSourceType.OtherId)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            await Task.WhenAll(LoadOrderAutoConfirmSettingAsync(), LoadOrderGeneralConfirmSettingsAsync());

            await base.HandleLoadedAsync();

            Title = "Настройки авто-согласования";
        }

        protected override async Task HandleOkAsync()
        {
            bool settingsChanged = _compareHelper.IsChanged();

            bool generalSettingsChanged = AllowableOnlyDontCall.IsChanged() || SalesHistoryDays.IsChanged() || SalesHistoryDeviation.IsChanged() || AllowSalesHistoryDeviation.IsChanged();

            if (!settingsChanged && !generalSettingsChanged)
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            if (settingsChanged && Settings.Any(x => IDataErrorInfoHelper.HasErrors(x, false)))
            {
                MessageFacadeService.ShowNotificationError("В таблице присутствуют ошибки");
                return;
            }

            if (settingsChanged && Settings.GroupBy(x => new { CarryId = x.Carry.Id, x.Payment.Id }).Count() != Settings.Count())
            {
                MessageFacadeService.ShowNotificationError("Дублируется связка Доставка-Оплата");
                return;
            }

            if (settingsChanged)
            {
                string successText = generalSettingsChanged ? string.Empty : "Настройки сохранены";

                IReadOnlyCollection<OrderAutoConfirmSettingDto> saveSettings = Settings.Select(x => new OrderAutoConfirmSettingDto(
                    x.Id,
                    x.Carry.Id,
                    x.Payment.Id,
                    x.MaxSumLimit!.Value,
                    x.MinExtraChargePercent!.Value,
                    x.MaxProductQuantity!.Value,
                    x.MaxLinesQuantity!.Value,
                    x.OrderProductSources.Cast<int>().ToArray())).ToList();

                await _errorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new UpdateOrderAutoConfirmSettings(saveSettings)), "сохранении настроек", successText, this, true, showNotification: !string.IsNullOrEmpty(successText));
            }

            if (generalSettingsChanged)
            {
                List<OrderGeneralConfirmSettingViewItem> generalSettings = new List<OrderGeneralConfirmSettingViewItem>();
                generalSettings.Add(SalesHistoryDays);
                generalSettings.Add(SalesHistoryDeviation);
                generalSettings.Add(AllowableOnlyDontCall);
                generalSettings.Add(AllowSalesHistoryDeviation);

                IReadOnlyCollection<SaveOrderGeneralConfirmSettingDto> saveOrderGeneralConfirmSettingDtos = generalSettings
                    .Select(
                        x => new SaveOrderGeneralConfirmSettingDto()
                        {
                            Id = x.Id,
                            Value = x.Value,
                            Active = x.Active
                        }).ToArray();

                await _errorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new UpdateOrderGeneralConfirmSettings(saveOrderGeneralConfirmSettingDtos)), "сохранении настроек", "Настройки сохранены", this, true);
            }

            CloseOk();
        }

        private void Add()
        {
            OrderAutoConfirmSettingViewItem newItem = new OrderAutoConfirmSettingViewItem();

            Settings.Insert(0, newItem);

            SelectedSetting = newItem;
        }

        private void Delete()
        {
            Settings.Remove(SelectedSetting);
        }

        private async Task LoadOrderAutoConfirmSettingAsync()
        {
            List<OrderAutoConfirmSettingDto> settings = await WebClient.ExecuteApiRequestAsync(new QueryOrderAutoConfirmSettings());

            Settings = settings.Select(x => _mapper.Map<OrderAutoConfirmSettingViewItem>(x)).ToObservableCollection();

            _compareHelper = new TelemartEnumerableCompareHelper<OrderAutoConfirmSettingViewItem>(Settings);
        }

        private async Task LoadOrderGeneralConfirmSettingsAsync()
        {
            List<OrderGeneralConfirmSettingDto> generalSettings = await WebClient.ExecuteApiRequestAsync(new QueryOrderGeneralConfirmSettings());

            SalesHistoryDays = _mapper.Map<OrderGeneralConfirmSettingViewItem>(generalSettings.Single(x => x.Id == OrderConfirmConstants.SalesHistoryDaysSettingId));
            SalesHistoryDeviation = _mapper.Map<OrderGeneralConfirmSettingViewItem>(generalSettings.Single(x => x.Id == OrderConfirmConstants.SalesHistoryDeviationSettingId));
            AllowableOnlyDontCall = _mapper.Map<OrderGeneralConfirmSettingViewItem>(generalSettings.Single(x => x.Id == OrderConfirmConstants.AllowableOnlyDontCallId));
            AllowSalesHistoryDeviation = _mapper.Map<OrderGeneralConfirmSettingViewItem>(generalSettings.Single(x => x.Id == OrderConfirmConstants.AllowSalesHistoryDeviationSettingId));

            SalesHistoryDays.SetValid(x => x.IntValue > 0 && x.IntValue < 367, "Значение должно быть в диапазоне 1..366 ");
            SalesHistoryDeviation.SetValid(x => x.IntValue >= 0 && x.IntValue <= 100, "Значение должно быть в диапазоне 0..100");
        }

        private void SalesHistoryDeviationChanged()
        {
            if (SalesHistoryDays != null && SalesHistoryDeviation != null)
            {
                SalesHistoryDays.Active = AllowSalesHistoryDeviation?.BoolValue ?? false;

                SalesHistoryDeviation.Active = AllowSalesHistoryDeviation?.BoolValue ?? false;

                RaisePropertiesChanged(nameof(SalesHistoryDays), nameof(SalesHistoryDeviation));
            }
        }
    }
}