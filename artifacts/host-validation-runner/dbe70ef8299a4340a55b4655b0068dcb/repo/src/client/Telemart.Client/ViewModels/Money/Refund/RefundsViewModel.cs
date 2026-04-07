using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Refund;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Money.Refund
{
    public sealed class RefundsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private List<CashboxDto> cashboxesList;
        private List<ContractorDto> contractorsList;

        public RefundsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            RefreshCommand = new AsyncCommand(RefreshAsync);
            AddCommand = new DelegateCommand(AddRefund);
            EditCommand = new DelegateCommand<RefundViewItem>(EditRefund, x => x != null);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);

            Messenger.Register<RefundMessage>(this, OnRefundMessage);
        }

        public RefundsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        #endregion

        #region INPC

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ObservableRangeCollection<RefundViewItem> Refunds
        {
            get { return GetProperty(() => Refunds); }
            private set { SetProperty(() => Refunds, value, () => { RaisePropertiesChanged(nameof(SumUah), nameof(SumUsd)); }); }
        }

        public RefundViewItem CurrentRefund
        {
            get { return GetProperty(() => CurrentRefund); }
            set { SetProperty(() => CurrentRefund, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        public decimal? SumUah => Refunds?.Where(r => r.Currency == Currency.Uah).Sum(r => r.Amount as decimal?);

        public decimal? SumUsd => Refunds?.Where(r => r.Currency == Currency.Usd).Sum(r => r.Amount as decimal?);

        #region Filter

        public DateTime? CreatedFrom
        {
            get { return GetProperty(() => CreatedFrom); }
            set { SetProperty(() => CreatedFrom, value); }
        }

        public DateTime? CreatedTo
        {
            get { return GetProperty(() => CreatedTo); }
            set { SetProperty(() => CreatedTo, value); }
        }

        public string RefundIds
        {
            get { return GetProperty(() => RefundIds); }
            set { SetProperty(() => RefundIds, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string OrderIds
        {
            get { return GetProperty(() => OrderIds); }
            set { SetProperty(() => OrderIds, value); }
        }

        public string ServiceRequestIds
        {
            get { return GetProperty(() => ServiceRequestIds); }
            set { SetProperty(() => ServiceRequestIds, value); }
        }

        public ObservableCollection<Payment> SelectedPayments
        {
            get { return GetProperty(() => SelectedPayments); }
            set { SetProperty(() => SelectedPayments, value); }
        }

        public List<object> SelectedCashboxes
        {
            get { return GetProperty(() => SelectedCashboxes); }
            set { SetProperty(() => SelectedCashboxes, value); }
        }

        public ObservableCollection<RefundState> SelectedStates
        {
            get { return GetProperty(() => SelectedStates); }
            set { SetProperty(() => SelectedStates, value); }
        }

        public bool? CompletedOnFiscalRegistrar
        {
            get { return GetProperty(() => CompletedOnFiscalRegistrar); }
            set { SetProperty(() => CompletedOnFiscalRegistrar, value); }
        }

        public ObservableRangeCollection<Payment> Payments { get; } = new ObservableRangeCollection<Payment>();

        public ObservableRangeCollection<ComboBoxItem> Cashboxes { get; } = new ObservableRangeCollection<ComboBoxItem>();

        public ObservableRangeCollection<RefundState> RefundStates { get; } = new ObservableRangeCollection<RefundState>();

        #endregion

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.Key)
            {
                case Key.Insert:
                    {
                        AddCommand.Execute(null);
                        handled = true;
                        break;
                    }

                case Key.F2:
                    {
                        EditCommand.Execute(CurrentRefund);
                        handled = true;
                        break;
                    }

                case Key.F5:
                    {
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    }

                case Key.F6:
                    {
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;
                    }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            Payments.AddRange(Dictionaries.GetItems<Payment>());
            RefundStates.AddRange(Dictionaries.GetItems<RefundState>());

            await Task.WhenAll(RefreshContractorsAsync(), RefreshCashboxesAsync());

            CancelFilteringCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            try
            {
                Refunds = null;

                await Task.WhenAll(RefreshContractorsAsync(), RefreshCashboxesAsync());

                IFilteringItem filteringItem = new RefundsFilteringItem
                {
                    CreatedFrom = CreatedFrom,
                    CreatedTo = CreatedTo,
                    RefundIds = RefundIds,
                    Fio = Fio,
                    Phone = Phone,
                    OrderIds = OrderIds,
                    ServiceRequestIds = ServiceRequestIds,
                    CompletedOnFiscalRegistrar = CompletedOnFiscalRegistrar,
                    Payments = SelectedPayments?.Select(x => x.Id).ToArray(),
                    Cashboxes = SelectedCashboxes?.Cast<ComboBoxItem>().Select(x => x.Id).ToArray(),
                    States = SelectedStates.Select(x => x.Id).ToArray()
                };

                PagedResult<RefundDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryRefunds(filteringItem));

                Refunds = pagedResult.Data.Select(x => Mapper.Map<RefundViewItem>(x)).ToObservableRangeCollection();
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to refresh Refunds");
                MessageFacadeService.ShowNotificationError(Resources.ServerConnectError);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh Refunds");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void AddRefund()
        {
            MessageFacadeService.ShowNotificationWarning("Not implemented");
        }

        private void EditRefund(RefundViewItem viewItem)
        {
            Messenger.Send(new RefundViewMessage(viewItem.Id));
        }

        private void CancelFiltering()
        {
            CreatedFrom = null;
            CreatedTo = null;
            RefundIds = null;
            OrderIds = null;
            ServiceRequestIds = null;
            Fio = null;
            Phone = null;
            SelectedPayments = null;
            SelectedCashboxes = null;
            CompletedOnFiscalRegistrar = null;
            SelectedStates = new ObservableCollection<RefundState> { RefundState.New, RefundState.Confirmed };

            RefreshCommand.Execute(null);
        }

        private void OnRefundMessage(RefundMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    {
                        Refunds.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                        break;
                    }
            }
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            if (!ReferenceEquals(contractors, contractorsList))
            {
                contractorsList = contractors;
                Contractors = contractors.ToReadOnlyObservableCollection();
            }
        }

        private async Task RefreshCashboxesAsync()
        {
            List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            if (!ReferenceEquals(cashboxes, cashboxesList))
            {
                cashboxesList = cashboxes;
                Cashboxes.Clear();
                Cashboxes.AddRange(cashboxesList.OrderBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)));
            }
        }
    }
}