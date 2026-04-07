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
using Telemart.Client.Business.Delivery;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.ServiceMovement;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ServiceMovement;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Service.ServiceMovements
{
    public class ServiceMovementsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public ServiceMovementsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            AddCommand = new DelegateCommand(Add);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            EditCommand = new DelegateCommand(Edit, () => SelectedMovement != null);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);
            PrintTtnCommand = new AsyncCommand(PrintTttAsync, CanExecutePrintTtn);

            Filter = new ServiceMovementsFilterViewModel(Dictionaries);

            Movements = new ObservableRangeCollection<ServiceMovementViewItem>();

            Messenger.Register<ServiceMovementMessage>(this, OnServiceMovementMessage);
        }

        #region Command

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        public IAsyncCommand PrintTtnCommand { get; }

        #endregion

        #region INPC

        public ServiceMovementsFilterViewModel Filter { get; }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            set { SetProperty(() => Warehouses, value); }
        }

        public ObservableRangeCollection<ServiceMovementViewItem> Movements
        {
            get { return GetProperty(() => Movements); }
            set { SetProperty(() => Movements, value); }
        }

        public ServiceMovementViewItem SelectedMovement
        {
            get { return GetProperty(() => SelectedMovement); }
            set { SetProperty(() => SelectedMovement, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

        private IDocumentManagerService NotModalSizeableDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

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
                        handled = true;
                        break;
                    case HotkeyMessageType.Add:
                        AddCommand.Execute(null);
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

        protected override Task HandleLoadedAsync()
        {
            IsSearchPanelClosed = false;
            CancelFilteringCommand.Execute(null);

            return Task.CompletedTask;
        }

        private async Task RefreshAsync()
        {
            try
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
                Warehouses = warehouses.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

                PagedResult<ServiceMovementSimpleDto> movements = await WebClient.ExecuteApiRequestAsync(new QueryServiceMovements(Filter.GetFilteringItem()));

                Movements.Clear();
                Movements.AddRange(movements.Data.Select(Mapper.Map<ServiceMovementViewItem>));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void Add()
        {
            SizeableDialogDocumentManagerService.ShowView<ServiceMovementCreateViewModel>(null, this);
        }

        private void Edit()
        {
            NotModalSizeableDocumentManagerService.ShowView<ServiceMovementViewModel>(new ServiceMovementParameter(SelectedMovement.Id), this);
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void OnServiceMovementMessage(ServiceMovementMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Movements.Insert(0, Mapper.Map<ServiceMovementViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    Movements.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => Mapper.Map(message.Entity, viewItem));
                    break;
            }
        }

        private async Task PrintTttAsync()
        {
            if (SelectedMovement.CarryId.HasValue)
            {
                ITrackNumberProvider trackNumberProvider = Dictionaries.GetItemById<CarryType>(SelectedMovement.CarryId.Value).GetTrackNumberProvider();

                await trackNumberProvider.PrintAsync(SelectedMovement.TrackNumber, true);
            }
        }

        private bool CanExecutePrintTtn()
        {
            return SelectedMovement is not null
                   && (SelectedMovement.CarryId == CarryType.NpDeliveryId
                       || SelectedMovement.CarryId == CarryType.NpWarehouseId)
                   && !string.IsNullOrWhiteSpace(SelectedMovement.TrackNumber);
        }
    }
}