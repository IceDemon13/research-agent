using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.SupplierCurrency;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.SupplierCurrency;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.SupplierCurrency
{
    public class SupplierCurrenciesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public SupplierCurrenciesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            ErrorHandler = errorHandler;

            Filter = new SupplierCurrenciesFilterViewModel(webClient);

            RefreshCommand = new AsyncCommand(RefreshAsync);
            SetCoursesCommand = new DelegateCommand(SetCourses);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);

            Messenger.Register<SupplierCurrenciesCreateMessage>(this, OnCreateNewSupplierCurrencyHistory);
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand SetCoursesCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        #endregion

        #region INPC

        public SupplierCurrenciesFilterViewModel Filter { get; }

        public ObservableRangeCollection<SupplierCurrencyItem> SupplierCurrencies
        {
            get { return GetProperty(() => SupplierCurrencies); }
            set { SetProperty(() => SupplierCurrencies, value); }
        }

        public SupplierCurrencyItem SelectedSupplierCurrency
        {
            get { return GetProperty(() => SelectedSupplierCurrency); }
            set { SetProperty(() => SelectedSupplierCurrency, value); }
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

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        #endregion

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        private IErrorHandler ErrorHandler { get; }

        private IDocumentManagerService NonModalDialogDocumentManagerService => GetService<IDocumentManagerService>("NonModalDialogDocumentManagerService");

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            switch (msg.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            Currencies = Dictionaries.GetCurrencies().ToReadOnlyObservableCollection();

            await Filter.RefreshAsync();

            IFilteringItem filteringItem = Filter.GetSupplierCurrenciesFilteringItem();

            await Task.WhenAll(
                RefreshContractorsAsync(),
                RefreshEmployeesAsync(),
                RefreshSupplierCurrensiesAsync(filteringItem));
        }

        private void SetCourses()
        {
            NonModalDialogDocumentManagerService.ShowView<CurrencySuppliersCreateViewModel>(null, this);
        }

        private async Task RefreshContractorsAsync()
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

        private async Task RefreshEmployeesAsync()
        {
            PagedResult<EmployeeDto> employeeResult = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true),
                "получении списка сотрудников",
                null,
                this,
                true,
                showNotification: false);

            if (employeeResult?.Data != null)
            {
                Employees = employeeResult.Data
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }
        }

        private async Task RefreshSupplierCurrensiesAsync(IFilteringItem filteringItem)
        {
            List<SupplierCurrencyRateHistoryDto> supplierCurrencyRateHistoryDtos = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QuerySupplierCurrencyRateHistory(filteringItem)),
                "получении курсов паставщиков",
                null,
                this,
                true,
                showNotification: false);

            if (supplierCurrencyRateHistoryDtos != null)
            {
                SupplierCurrencies = supplierCurrencyRateHistoryDtos.Select(x => Mapper.Map<SupplierCurrencyItem>(x)).ToObservableRangeCollection();
            }
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void OnCreateNewSupplierCurrencyHistory(SupplierCurrenciesCreateMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    SupplierCurrencies.AddRange(message.SupplierCurrencyRateHistoryDtos.Select(x => Mapper.Map<SupplierCurrencyItem>(x)));
                    break;
            }
        }
    }
}