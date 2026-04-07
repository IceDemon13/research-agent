using System;
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
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.SupplierBill;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.SupplierBill.Create;

namespace Telemart.Client.ViewModels.SupplierBill
{
    public sealed class SupplierBillsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public SupplierBillsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;

            AddCommand = new DelegateCommand(Add);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            EditCommand = new DelegateCommand<SupplierBillViewItem>(Edit, x => x != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Filter = new SupplierBillFilterViewModel(webClient, dictionaries);

            Messenger.Register<SupplierBillMessage>(this, OnSupplierBillMessage);

            Bills = new ObservableRangeCollection<SupplierBillViewItem>();
        }

        public SupplierBillsViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        public SupplierBillFilterViewModel Filter { get; }

        public ObservableRangeCollection<SupplierBillViewItem> Bills
        {
            get { return GetProperty(() => Bills); }
            private set { SetProperty(() => Bills, value); }
        }

        public SupplierBillViewItem SelectedBill
        {
            get { return GetProperty(() => SelectedBill); }
            set { SetProperty(() => SelectedBill, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Suppliers
        {
            get { return GetProperty(() => Suppliers); }
            set { SetProperty(() => Suppliers, value); }
        }

        public ReadOnlyObservableCollection<SupplierBillState> States
        {
            get { return GetProperty(() => States); }
            set { SetProperty(() => States, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            set { SetProperty(() => Currencies, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        private IDialogService WizardDialogService => GetService<IDialogService>("CreateSupplierBillWizardDialogService", ServiceSearchMode.LocalOnly);

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

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
                        break;
                    case HotkeyMessageType.Add:
                        AddCommand.Execute(null);
                        break;
                    case HotkeyMessageType.Edit:
                        EditCommand.Execute(SelectedBill);
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
            RaisePropertyChanged(nameof(IsSearchPanelClosed));

            States = Dictionaries.GetItems<SupplierBillState>().ToReadOnlyObservableCollection();
            Currencies = Dictionaries.GetItems<Currency>().ToReadOnlyObservableCollection();

            await Filter.RefreshAsync();

            CancelFilteringCommand.Execute(null);
        }

        private void Add()
        {
            CreateSupplierBillModel model = new CreateSupplierBillModel();

            WizardDialogViewModel<CreateSupplierBillModel> wizardDialogViewModel = new WizardDialogViewModel<CreateSupplierBillModel>(
                typeof(CreateSupplierBillContractorPageViewModel),
                model,
                this);

            WizardDialogService.ShowDialog(MessageButton.OKCancel, "Создание счета поставщика", wizardDialogViewModel);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void Edit(SupplierBillViewItem bill)
        {
            Messenger.Send(new SupplierBillViewMessage(bill.Id));
        }

        private async Task RefreshAsync()
        {
            try
            {
                await RefreshSuppliersAsync();

                await Filter.RefreshAsync();

                PagedResult<SupplierBillDto> bills = await WebClient.ExecuteApiRequestAsync(new QuerySupplierBills(Filter.GetFilteringItem()));

                Bills.Clear();
                Bills.AddRange(bills.Data.Select(x => Mapper.Map<SupplierBillViewItem>(x)));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{messageError}.", Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private async Task RefreshSuppliersAsync()
        {
            PagedResult<ContractorDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true);

            Suppliers = pagedResult.Data
                .Where(x => !x.IsFolder && x.IsSupplier)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private void OnSupplierBillMessage(SupplierBillMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Bills.Insert(0, Mapper.Map<SupplierBillViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    Bills.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }
    }
}