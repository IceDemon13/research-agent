using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Inventory;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Warehouse.Inventory
{
    public sealed class WarehouseInventoriesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public WarehouseInventoriesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

            Filter = new WarehouseInventoriesFilterViewModel(webClient);

            RefreshCommand = new AsyncCommand(RefreshAsync);
            AddCommand = new DelegateCommand(AddInventory);
            EditCommand = new DelegateCommand(EditInventory, () => SelectedInventory != null);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);

            Messenger.Register<InventoryMessage>(this, OnInventoryMessage);
        }

        public WarehouseInventoriesViewModel()
        {
        }

        #region INPC

        public WarehouseInventoriesFilterViewModel Filter { get; }

        public ObservableCollection<InventoryViewItem> Inventories
        {
            get { return GetProperty(() => Inventories); }
            set { SetProperty(() => Inventories, value); }
        }

        public InventoryViewItem SelectedInventory
        {
            get { return GetProperty(() => SelectedInventory); }
            set { SetProperty(() => SelectedInventory, value); }
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

        #endregion

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private IDocumentManagerService DialogDocumentManagerService
            => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService
            => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private Dictionary<int, WarehouseSimpleDto> WarehousesDictionary { get; set; }

        private Dictionary<int, EmployeeDto> EmployeesDictionary { get; set; }

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;
            if (hotkeyMessage.ModifierKeys == ModifierKeys.Alt)
            {
                switch (hotkeyMessage.Key)
                {
                    case Key.L:
                        IsSearchPanelClosed = !IsSearchPanelClosed;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (hotkeyMessage.HotkeyMessageType)
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

                    case HotkeyMessageType.Add:
                        AddCommand.Execute(null);
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            IsSearchPanelClosed = false;
            await Filter.RefreshAsync();
            CancelFilteringCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            try
            {
                await Filter.RefreshAsync();

                InventoryFilteringItem inventoryFilteringItem = Filter.GetInventoryFilteringItem();
                PagedResult<InventoryDto> inventories = await WebClient.ExecuteApiRequestAsync(new QueryInventories(inventoryFilteringItem));
                List<InventoryViewItem> inventoriesViewItems = Mapper.Map<List<InventoryViewItem>>(inventories.Data);
                await FillInnerObjectsAsync(inventoriesViewItems);

                Inventories = new ObservableCollection<InventoryViewItem>(inventoriesViewItems);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to refresh(load) inventory");
                MessageFacadeService.ShowNotificationError("Ошибка при загрузке данных");
            }
        }

        private async Task FillInnerObjectsAsync(ICollection<InventoryViewItem> inventories)
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
            WarehousesDictionary = Mapper.Map<List<WarehouseSimpleDto>>(warehouses).ToDictionary(x => x.Id);

            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
            EmployeesDictionary = employees.ToDictionary(x => x.Id);

            foreach (InventoryViewItem inventory in inventories)
            {
                FillInventoryInnerObjects(inventory);
            }
        }

        private void FillInventoryInnerObjects(InventoryViewItem inventory)
        {
            if (EmployeesDictionary.TryGetValue(inventory.CreatedByEmployeeId, out EmployeeDto createdByEmployee))
            {
                inventory.CreatedByEmployeeName = createdByEmployee.Name;
            }

            if (EmployeesDictionary.TryGetValue(inventory.EmployeeLockId ?? 0, out EmployeeDto employeeLock))
            {
                inventory.EmployeeLock = new EmployeeSimpleDto
                {
                    Id = employeeLock.Id,
                    Name = employeeLock.Name,
                    Login = employeeLock.Login,
                    ShortName = employeeLock.ShortName
                };
            }

            if (WarehousesDictionary.TryGetValue(inventory.WarehouseId, out WarehouseSimpleDto warehouse))
            {
                inventory.Warehouse = warehouse;
            }
        }

        private void AddInventory()
        {
            CreateInventoryViewModel viewModel = DialogDocumentManagerService.ShowView<CreateInventoryViewModel>(null, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            InventoryViewItem inventory = Mapper.Map<InventoryViewItem>(viewModel.CreatedInventory);
            FillInventoryInnerObjects(inventory);

            foreach (InventoryViewItem groupInventory in inventory.GroupInventories)
            {
                FillInventoryInnerObjects(groupInventory);
                Inventories.Insert(0, groupInventory);
            }

            Inventories.Insert(0, inventory);
            SelectedInventory = inventory;
        }

        private void EditInventory()
        {
            SizeableDialogDocumentManagerService.ShowView<InventoryViewModel>(SelectedInventory.Id, this);
        }

        private void OnInventoryMessage(InventoryMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    {
                        Inventories.DoActionWithItem(x => x.Id == message.Entity.Id, x => Mapper.Map(message.Entity, x));
                        break;
                    }
            }
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }
    }
}