using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.SupplierCurrency;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.SupplierCurrency;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.SupplierCurrency
{
    public sealed class CurrencySuppliersCreateViewModel : TelemartDialogViewModelBase
    {
        public CurrencySuppliersCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
            Messenger = messenger;

            AddSupplierCurrencyCommand = new DelegateCommand(AddSupplierCurrency);
            HandleSupplierChangedCommand = new DelegateCommand<CellValueChangedEventArgs>(SupplierChanged);
            DeleteSupplierCurrencyCommand = new DelegateCommand(DeleteSupplierCurrency, () => SelectedItem != null);
        }

        #region Commands

        public IDelegateCommand AddSupplierCurrencyCommand { get; }

        public IDelegateCommand DeleteSupplierCurrencyCommand { get; }

        public IDelegateCommand HandleSupplierChangedCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<SupplierCurrencyCreateItem> SupplierCurrencyCreateItems
        {
            get { return GetProperty(() => SupplierCurrencyCreateItems); }
            set { SetProperty(() => SupplierCurrencyCreateItems, value); }
        }

        public SupplierCurrencyCreateItem SelectedItem
        {
            get { return GetProperty(() => SelectedItem); }
            set { SetProperty(() => SelectedItem, value, () => RaisePropertyChanged(nameof(IsNewLine))); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Suppliers
        {
            get { return GetProperty(() => Suppliers); }
            private set { SetProperty(() => Suppliers, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        public bool IsNewLine => SelectedItem is { Id: > 0 };

        private ReadOnlyObservableCollection<SupplierCurrencyRateActualDto> SupplierCurrencyRateActualDtos
        {
            get { return GetProperty(() => SupplierCurrencyRateActualDtos); }
            set { SetProperty(() => SupplierCurrencyRateActualDtos, value); }
        }

        #endregion

        private IErrorHandler ErrorHandler { get; }

        private IMessenger Messenger { get; }

        protected override async Task HandleLoadedAsync()
        {
            Title = "Задать курсы";

            Currencies = Dictionaries.GetCurrencies().ToReadOnlyObservableCollection();

            SupplierCurrencyCreateItems = Array.Empty<SupplierCurrencyCreateItem>().ToObservableCollection();

            await LoadContractorsAsync();

            await LoadSupplierCurrenciesRateActualAsync();
        }

        protected override async Task HandleOkAsync()
        {
            List<SupplierCurrencyCreateItem> changedItems = SupplierCurrencyCreateItems.Where(x => x.IsChanged).ToList();

            if (changedItems.Count == 0)
            {
                MessageFacadeService.ShowMessageBoxWarning("Нет измененений для сохранения");

                return;
            }

            SupplierCurrencyRateCreateDto[] createDtos = changedItems.Select(MapToSupplierCurrencyRateCreateDto).ToArray();

            SupplierCurrencyRatesCreateDto currencyRatesCreateDto = new SupplierCurrencyRatesCreateDto(createDtos);

            Result<List<SupplierCurrencyRateHistoryDto>> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateSupplierCurrencyRates(currencyRatesCreateDto)),
                "обновлении актуальных курсов поставщиков",
                "Обновление курсов поставщиков выполнено",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                Messenger.Send(new SupplierCurrenciesCreateMessage(result.Data.ToReadOnlyObservableCollection(), MessageType.Added));

                CloseOk();
            }
        }

        private static SupplierCurrencyRateCreateDto MapToSupplierCurrencyRateCreateDto(SupplierCurrencyCreateItem item)
        {
            return new SupplierCurrencyRateCreateDto(item.Id, item.SupplierId, item.CurrencyId, item.RateNew);
        }

        private static IReadOnlyCollection<SupplierCurrencyCreateItem> GetDefaultSupplierCurrencyCreateItems(
            IReadOnlyCollection<SupplierCurrencyRateActualDto> supplierCurrencyRateDtos,
            IReadOnlyCollection<ContractorDto> suppliers)
        {
            List<SupplierCurrencyCreateItem> result = new List<SupplierCurrencyCreateItem>();

            IReadOnlyDictionary<int, ContractorCurrencyPermissionDto[]> contractorCurrencyPermissionDictionary = suppliers?
                .Where(x => x.CurrencyPermissions?.Any(y => y.CurrencyControl) == true)
                .ToDictionary(
                    key => key.Id,
                    contr => contr.CurrencyPermissions?.Where(x => x.CurrencyControl).ToArray());

            if (contractorCurrencyPermissionDictionary != null)
            {
                foreach (KeyValuePair<int, ContractorCurrencyPermissionDto[]> contractorPermissions in contractorCurrencyPermissionDictionary)
                {
                    int[] currencyIds = supplierCurrencyRateDtos
                        .Where(x => x.SupplierId == contractorPermissions.Key)
                        .Select(y => y.CurrencyId)
                        .ToArray();

                    string nameContractor = suppliers.FirstOrDefault(x => x.Id == contractorPermissions.Key)?.Name;

                    SupplierCurrencyCreateItem[] supplierCurrencyByCurrencyControlItems = contractorPermissions.Value
                        .Where(x => !currencyIds.Contains(x.CurrencyId))
                        .Select(x => new SupplierCurrencyCreateItem(contractorPermissions.Key, x.CurrencyId) { SupplerName = nameContractor })
                        .ToArray();

                    result.AddRange(supplierCurrencyByCurrencyControlItems);
                }
            }

            return result;
        }

        private async Task LoadContractorsAsync()
        {
            PagedResult<ContractorDto> contractorsResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryContractors()),
                "получении списка поставщиков",
                null,
                this,
                true,
                showNotification: false);

            if (contractorsResult?.Data != null)
            {
                Suppliers = contractorsResult.Data
                    .Where(x => x.IsSupplier && x.Active && !x.IsFolder)
                    .OrderBy(x => x.Name)
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task LoadSupplierCurrenciesRateActualAsync()
        {
            List<SupplierCurrencyRateActualDto> supplierCurrencyRateDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QuerySupplierCurrencyRateActual(null)),
                "получении курсов паставщиков",
                null,
                this,
                true,
                showNotification: false);

            SupplierCurrencyRateActualDtos = supplierCurrencyRateDtos.ToReadOnlyObservableCollection();

            List<SupplierCurrencyCreateItem> items = supplierCurrencyRateDtos
                .Select(MapToSupplierCurrencyCreateItem)
                .ToList();

            IReadOnlyCollection<SupplierCurrencyCreateItem> defaultItems = GetDefaultSupplierCurrencyCreateItems(supplierCurrencyRateDtos, Suppliers);

            items.AddRange(defaultItems);

            SupplierCurrencyCreateItems = items.OrderBy(x => x.SupplerName).ToObservableCollection();
        }

        private void AddSupplierCurrency()
        {
            SupplierCurrencyCreateItem newItem = new SupplierCurrencyCreateItem();

            SelectedItem = null;

            SupplierCurrencyCreateItems.Insert(0, newItem);
        }

        private void SupplierChanged(CellValueChangedEventArgs e)
        {
            if (SelectedItem != null && e.Row is SupplierCurrencyCreateItem)
            {
                string fieldName = e.Column.FieldName;

                if (fieldName == nameof(SupplierCurrencyCreateItem.SupplierId) || fieldName == nameof(SupplierCurrencyCreateItem.CurrencyId))
                {
                    SupplierCurrencyRateActualDto dto = SupplierCurrencyRateActualDtos?
                        .FirstOrDefault(x => x.SupplierId == SelectedItem.SupplierId && x.CurrencyId == SelectedItem.CurrencyId);

                    SelectedItem.RateOld = dto?.Rate ?? 0;

                    int? count = SupplierCurrencyCreateItems?
                        .Count(x => x.CurrencyId == SelectedItem.CurrencyId && x.SupplierId == SelectedItem.SupplierId);

                    SelectedItem.IsCopySupplierCurrencyRate = count > 1;
                }
            }
        }

        private void DeleteSupplierCurrency()
        {
            SupplierCurrencyCreateItems.Remove(SelectedItem);
            SelectedItem = null;
        }

        private SupplierCurrencyCreateItem MapToSupplierCurrencyCreateItem(SupplierCurrencyRateActualDto dto)
        {
            SupplierCurrencyCreateItem item = new SupplierCurrencyCreateItem(dto.Id, dto.SupplierId, dto.CurrencyId, dto.Rate);

            item.SupplerName = Suppliers?.FirstOrDefault(x => x.Id == item.Id)?.Name;

            return item;
        }
    }
}