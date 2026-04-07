using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Data;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Customer;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Customer;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Customer
{
    public sealed class CustomersViewModel : TelemartViewModelBase, ISupportHotkeys, IDisposable
    {
        private const int PageSize = 50;

        private List<ContractorDto> contractorsList;

        public CustomersViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            CanEdit = WebClient.IsOperationAllowed(BusinessOperation.CustomerUpdate);

            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand(Edit, () => SelectedCustomer != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Filter = new CustomerFilterViewModel(webClient);

            Customers = new PagedAsyncSource();

            Customers.PageSize = PageSize;
            Customers.ElementType = typeof(CustomerViewItem);
            Customers.PageNavigationMode = PageNavigationMode.ArbitraryWithTotalPageCount;

            Customers.FetchPage += CustomersOnFetchPage;
            Customers.GetTotalSummaries += CustomersOnGetTotalSummaries;
        }

        public CustomersViewModel()
        {
        }

        #region Commands

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public bool CanEdit
        {
            get { return GetProperty(() => CanEdit); }
            private set { SetProperty(() => CanEdit, value); }
        }

        public CustomerFilterViewModel Filter { get; }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public PagedAsyncSource Customers
        {
            get { return GetProperty(() => Customers); }
            private set { SetProperty(() => Customers, value); }
        }

        public CustomerViewItem SelectedCustomer
        {
            get { return GetProperty(() => SelectedCustomer); }
            set { SetProperty(() => SelectedCustomer, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public long? TotalCount
        {
            get { return GetProperty(() => TotalCount); }
            private set { SetProperty(() => TotalCount, value); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        public void Dispose()
        {
            Customers.Dispose();
        }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;

                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(null);
                        handled = true;
                        break;

                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            IsSearchPanelClosed = false;

            Filter.ResetFilterValues();

            try
            {
                await RefreshContractorsAsync();

                await Filter.RefreshAsync();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            CanEdit = true;
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void Edit()
        {
            DialogDocumentManagerService.ShowView<CustomerViewModel>(new CustomerParameter(SelectedCustomer.Id), this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                await RefreshContractorsAsync();

                await Filter.RefreshAsync();

                Customers.PageIndex = 0;
                Customers.RefreshRows();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            if (ReferenceEquals(contractors, contractorsList))
            {
                return;
            }

            contractorsList = contractors;

            Contractors = contractorsList
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private void CustomersOnGetTotalSummaries(object sender, GetSummariesAsyncEventArgs e)
        {
            e.Result = Task.FromResult(e.Summaries
                .Select(x => x.SummaryType == SummaryType.Count ? TotalCount : null)
                .Cast<object>()
                .ToArray());
        }

        private void CustomersOnFetchPage(object sender, FetchPageAsyncEventArgs e)
        {
            e.Result = GetCustomersAsync(e.Skip, e.Take);
        }

        private async Task<FetchRowsResult> GetCustomersAsync(int skip, int take)
        {
            PagedResult<CustomerDto> customersPage = await WebClient.ExecuteApiRequestAsync(new QueryCustomers(Filter.GetFilteringItem(skip, take)));

            TotalCount = customersPage.Pagination.TotalCount;

            Customers.UpdateSummaries();

            return new FetchRowsResult(
                customersPage.Data.Select(x => Mapper.Map<CustomerViewItem>(x)).Cast<object>().ToArray(),
                customersPage.Pagination.HasMoreRows);
        }
    }
}