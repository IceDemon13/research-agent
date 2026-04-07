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
using Telemart.Client.Data.Requests.Features.AssemblyFullRule;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyFullRule;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.AssemblyFullRule
{
    public class AssemblyFullRulesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        public AssemblyFullRulesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            CanCreate = WebClient.IsOperationAllowed(BusinessOperation.AssemblyFullRuleCreate);

            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedRule != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);

            Rules = new ObservableRangeCollection<AssemblyFullRuleViewItem>();

            Messenger.Register<AssemblyFullRuleMessage>(this, OnAssemblyFullRuleMessage);
        }

        public AssemblyFullRulesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand AddCommand { get; }

        public IDelegateCommand EditCommand { get; }

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

        public ReadOnlyObservableCollection<AssemblyFullRuleOperation> Operations
        {
            get { return GetProperty(() => Operations); }
            private set { SetProperty(() => Operations, value); }
        }

        public ObservableRangeCollection<AssemblyFullRuleViewItem> Rules
        {
            get { return GetProperty(() => Rules); }
            set { SetProperty(() => Rules, value); }
        }

        public AssemblyFullRuleViewItem SelectedRule
        {
            get { return GetProperty(() => SelectedRule); }
            set { SetProperty(() => SelectedRule, value); }
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
            Operations = Dictionaries.GetItems<AssemblyFullRuleOperation>().ToReadOnlyObservableCollection();

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
            DialogDocumentManagerService.ShowView<AssemblyFullRuleViewModel>(new AssemblyFullRuleParameter(0), this);
        }

        private void Edit()
        {
            DialogDocumentManagerService.ShowView<AssemblyFullRuleViewModel>(new AssemblyFullRuleParameter(SelectedRule.Id), this);
        }

        private async Task RefreshAsync()
        {
            try
            {
                await RefreshCategories();

                List<AssemblyFullRuleDto> rules = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyFullRules());

                Rules.Clear();

                Rules.AddRange(rules.Select(x => Mapper.Map<AssemblyFullRuleViewItem>(x)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to edit additional service product date");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }

            async Task RefreshCategories()
            {
                List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

                Categories = categories.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }
        }

        private void OnAssemblyFullRuleMessage(AssemblyFullRuleMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Rules.Insert(0, Mapper.Map<AssemblyFullRuleViewItem>(message.Entity));

                    break;
                case MessageType.Changed:
                    Rules.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => { Mapper.Map(message.Entity, viewItem); });

                    break;
            }
        }
    }
}
