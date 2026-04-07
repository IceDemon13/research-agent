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
using Telemart.Client.Data.Requests.Features.Accessory;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Accessory;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Accessory
{
    public class AccessoriesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public AccessoriesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            CanCreate = WebClient.IsOperationAllowed(BusinessOperation.AccessoryCreate);

            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedAccessory != null);
            CopyCommand = new DelegateCommand(Copy, () => SelectedAccessory != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Accessories = new ObservableRangeCollection<AccessoryViewItem>();

            Messenger.Register<AccessoryMessage>(this, OnAccessoryMessage);
        }

        public AccessoriesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand CopyCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        #endregion

        #region INPC

        public bool CanCreate
        {
            get { return GetProperty(() => CanCreate); }
            private set { SetProperty(() => CanCreate, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public ObservableRangeCollection<AccessoryViewItem> Accessories
        {
            get { return GetProperty(() => Accessories); }
            set { SetProperty(() => Accessories, value); }
        }

        public AccessoryViewItem SelectedAccessory
        {
            get { return GetProperty(() => SelectedAccessory); }
            set { SetProperty(() => SelectedAccessory, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        #endregion

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

                        if (CanCreate)
                        {
                            AddCommand.Execute(null);
                            handled = true;
                        }

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

            RefreshCommand.Execute(null);

            return Task.CompletedTask;
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            CanCreate = true;
        }

        private void Add()
        {
            SizeableDialogDocumentManagerService.ShowView<AccessoryViewModel>(new AccessoryParameter(0), this);
        }

        private void Edit()
        {
            SizeableDialogDocumentManagerService.ShowView<AccessoryViewModel>(new AccessoryParameter(SelectedAccessory.Id), this);
        }

        private void Copy()
        {
            SizeableDialogDocumentManagerService.ShowView<AccessoryViewModel>(new AccessoryParameter(SelectedAccessory.Id, true), this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                await RefreshCategories();

                List<AccessoryDto> accessories = await WebClient.ExecuteApiRequestAsync(new QueryAccessories());

                Accessories.Clear();

                Accessories.AddRange(accessories.Select(x => Mapper.Map<AccessoryViewItem>(x)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, Resources.ErrorDuringDataLoading);
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            async Task RefreshCategories()
            {
                List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

                Categories = categories.Select(x => new ComboBoxItem(x.Id, x.FullName)).ToReadOnlyObservableCollection();
            }
        }

        private void OnAccessoryMessage(AccessoryMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    AccessoryViewItem newAccessory = Mapper.Map<AccessoryViewItem>(message.Entity);
                    Accessories.Insert(0, newAccessory);

                    break;
                case MessageType.Changed:
                    Accessories.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem =>
                    {
                        Mapper.Map(message.Entity, viewItem);
                    });

                    break;
            }
        }
    }
}
