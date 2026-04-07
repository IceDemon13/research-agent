using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Payments;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Payments;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Payments
{
    public class PaymentsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public PaymentsViewModel(
              IWebClient webClient,
              IDictionaries dictionaries,
              IMessageFacadeService messageFacadeService,
              IMapper mapper,
              IMessenger messenger)
              : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            EditCommand = new DelegateCommand(Edit, () => SelectedPayment != null && WebClient.IsOperationAllowed(BusinessOperation.PaymentTypeUpdate));
            EditCreditOffersCommand = new DelegateCommand(EditCreditOffers, () => WebClient.IsOperationAllowed(BusinessOperation.PaymentTypeModuleAccess));
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Messenger.Register<PaymentMessage>(this, OnPaymentTypeMessage);

            Payments = new ObservableRangeCollection<PaymentViewItem>();
        }

        public PaymentsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand EditCreditOffersCommand { get; }

        #endregion

        #region INPC

        public ObservableRangeCollection<PaymentViewItem> Payments
        {
            get { return GetProperty(() => Payments); }
            set { SetProperty(() => Payments, value); }
        }

        public PaymentViewItem SelectedPayment
        {
            get { return GetProperty(() => SelectedPayment); }
            set { SetProperty(() => SelectedPayment, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;
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

            return handled;
        }

        protected override Task HandleLoadedAsync()
        {
            return RefreshAsync();
        }

        protected override void OnInitializeInDesignMode()
        {
            Payments = new ObservableRangeCollection<PaymentViewItem>(new[]
            {
                new PaymentViewItem { Name = "Способ оплаты 1", LimitUah = 0, LimitUsd = 10000 },
                new PaymentViewItem { Name = "Способ оплаты 2", LimitUah = 10000, LimitUsd = 0 },
                new PaymentViewItem { Name = "Способ оплаты 3", LimitUah = 99999999, LimitUsd = 10000 },
                new PaymentViewItem { Name = "Способ оплаты 4", LimitUah = 56785687, LimitUsd = 10567000 },
            });
        }

        private void Edit()
        {
            DialogDocumentManagerService.ShowView<PaymentViewModel>(new PaymentParameter(SelectedPayment.Id), this);
        }

        private void EditCreditOffers()
        {
            DialogDocumentManagerService.ShowView<CreditOffersViewModel>(null, this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                List<PaymentDto> payments = await WebClient.ExecuteApiRequestAsync(new QueryPayments());

                Payments.Clear();

                Payments.AddRange(payments
                    .OrderBy(x => x.Id)
                    .Select(x => Mapper.Map<PaymentViewItem>(x)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnPaymentTypeMessage(PaymentMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    Payments.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }
    }
}
