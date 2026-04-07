using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Cashbox
{
    public sealed class CashboxesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private IReadOnlyDictionary<int, ComboBoxItem> employeesDictionary;

        public CashboxesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedCashbox != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Messenger.Register<CashboxMessage>(this, OnCashboxMessage);
        }

        public CashboxesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ReadOnlyObservableCollection<CashboxType> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        public ObservableRangeCollection<CashboxViewItem> Cashboxes
        {
            get { return GetProperty(() => Cashboxes); }
            set { SetProperty(() => Cashboxes, value); }
        }

        public CashboxViewItem SelectedCashbox
        {
            get { return GetProperty(() => SelectedCashbox); }
            set { SetProperty(() => SelectedCashbox, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool CanCreate
        {
            get { return GetProperty(() => CanCreate); }
            private set { SetProperty(() => CanCreate, value); }
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
                case HotkeyMessageType.Add:
                    if (CanCreate)
                    {
                        AddCommand.Execute(null);
                    }

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
            CanCreate = WebClient.IsOperationAllowed(BusinessOperation.CashboxCreate);

            Types = Dictionaries.GetItems<CashboxType>().ToReadOnlyObservableCollection();
            Subdivisions = Dictionaries.GetItems<Subdivision>().ToReadOnlyObservableCollection();
            Currencies = Dictionaries.GetCurrencies().ToReadOnlyObservableCollection();
            Cashboxes ??= new ObservableRangeCollection<CashboxViewItem>();

            RefreshCommand.Execute(null);

            return Task.CompletedTask;
        }

        private void Add()
        {
            DialogDocumentManagerService.ShowView<CashboxViewModel>(new CashboxEditParameter(0), this);
        }

        private void Edit()
        {
            DialogDocumentManagerService.ShowView<CashboxViewModel>(new CashboxEditParameter(SelectedCashbox.Id), this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                Cashboxes.Clear();

                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                employeesDictionary = employees.ToDictionary(x => x.Id, y => new ComboBoxItem(y.Id, y.Name, y.Active));

                List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes());

                Cashboxes.AddRange(cashboxes
                    .OrderByDescending(x => x.Id)
                    .Select(x => Map(x, new CashboxViewItem())));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void OnCashboxMessage(CashboxMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Cashboxes.Insert(0, Map(message.Entity, new CashboxViewItem()));
                    break;
                case MessageType.Changed:
                    Cashboxes.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Map(message.Entity, viewItem));
                    break;
            }
        }

        private CashboxViewItem Map(CashboxDto dto, CashboxViewItem viewItem)
        {
            CashboxViewItem mappedItem = Mapper.Map(dto, viewItem);

            mappedItem.Employee = employeesDictionary.GetValueOrDefault(dto.EmployeeId);

            return mappedItem;
        }
    }
}