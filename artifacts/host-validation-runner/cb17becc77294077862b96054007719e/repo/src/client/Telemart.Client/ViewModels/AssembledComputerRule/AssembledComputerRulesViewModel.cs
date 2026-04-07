using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Layouts;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.AssembledComputerRule;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Store;

namespace Telemart.Client.ViewModels.AssembledComputerRule
{
    public sealed class AssembledComputerRulesViewModel : ViewModelBase, ISupportHotkeys
    {
        public AssembledComputerRulesViewModel(
            IWebClient webClient,
            IMessenger messenger,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            ILogger<AssembledComputerRulesViewModel> logger,
            IFilterModuleLayoutService<AssembledComputerRuleFilteringItem> filterModuleLayoutService)
        : this()
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Logger = logger;
            FilterModuleLayoutService = filterModuleLayoutService;

            Messenger.Register<EntityMessage<AssembledComputerRuleDto>>(this, OnAssembledComputerRuleMessage);
        }

        public AssembledComputerRulesViewModel()
        {
            RefreshCommand = new AsyncCommand(RefreshAsync);
            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            EditCommand = new DelegateCommand(Edit, () => SelectedAssembledComputerRule != null && WebClient.IsOperationAllowed(BusinessOperation.AssembledComputerRuleUpdate));
            CreateCommand = new DelegateCommand(Create, () => WebClient.IsOperationAllowed(BusinessOperation.AssembledComputerRuleCreate));
            ShowReserveCommand = new DelegateCommand(ShowReserve);
            CancelFilteringCommand = new DelegateCommand(CancelFiltering);

            Filter = new AssembledComputerRulesFilterViewModel();
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand CreateCommand { get; }

        public IDelegateCommand HandleLoadedCommand { get; }

        public IDelegateCommand ShowReserveCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

        #endregion

        #region INPC

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        public bool IsSearchPanelClosed
        {
            get { return GetProperty(() => IsSearchPanelClosed); }
            set { SetProperty(() => IsSearchPanelClosed, value); }
        }

        public ObservableCollection<AssembledComputerRulesViewItem> AssembledComputerRules
        {
            get { return GetProperty(() => AssembledComputerRules); }
            set { SetProperty(() => AssembledComputerRules, value); }
        }

        public AssembledComputerRulesViewItem SelectedAssembledComputerRule
        {
            get { return GetProperty(() => SelectedAssembledComputerRule); }
            set { SetProperty(() => SelectedAssembledComputerRule, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public AssembledComputerRulesFilterViewModel Filter { get; }

        public IFilterModuleLayoutService<AssembledComputerRuleFilteringItem> FilterModuleLayoutService { get; }

        #endregion

        private IWebClient WebClient { get; }

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private ILogger<AssembledComputerRulesViewModel> Logger { get; }

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
                        EditCommand.Execute(SelectedAssembledComputerRule);
                        handled = true;
                        break;

                    case HotkeyMessageType.ShowColumnChooser:
                        IsColumnChooserVisible = !IsColumnChooserVisible;
                        handled = true;
                        break;

                    case HotkeyMessageType.Add:
                        CreateCommand.Execute(null);
                        handled = true;
                        break;
                }
            }

            return handled;
        }

        private async Task RefreshAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                AssembledComputerRules = null;

                List<AssembledComputerRuleDto> assembledComputerRules = await WebClient.ExecuteApiRequestAsync(new QueryFilteredAssembledComputerRules(Filter.GetFilteringItem()));

                AssembledComputerRules = assembledComputerRules.Select(x => Mapper.Map<AssembledComputerRulesViewItem>(x)).ToObservableCollection();
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
                Logger.LogError(exception, "Exception while refreshing grid.");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private void CancelFiltering()
        {
            Filter.ResetFilterValues();
            RefreshCommand.Execute(null);
        }

        private void HandleLoaded()
        {
            IsSearchPanelClosed = false;

            if (AssembledComputerRules == null)
            {
                AssembledComputerRules = new ObservableCollection<AssembledComputerRulesViewItem>();
            }

            if (AssembledComputerRules.Any())
            {
                return;
            }

            RefreshCommand.Execute(null);
        }

        private void Edit()
        {
            SizeableDialogDocumentManagerService.ShowView<AssembledComputerRuleViewModel>(new AssembledComputerRuleParameter(SelectedAssembledComputerRule.Id, Filter.GetFilteringItem().ProductName), this);
        }

        private void Create()
        {
            SizeableDialogDocumentManagerService.ShowView<AssembledComputerRuleViewModel>(new AssembledComputerRuleParameter(0), this);
        }

        private void ShowReserve()
        {
            SizeableDialogDocumentManagerService.ShowView<AssembledComputerRuleReserveViewModel>(null, this);
        }

        private void OnAssembledComputerRuleMessage(EntityMessage<AssembledComputerRuleDto> message)
        {
            AssembledComputerRuleDto dto = message.Entity;

            switch (message.MessageType)
            {
                case MessageType.Added:
                    AssembledComputerRules?.Add(Mapper.Map<AssembledComputerRulesViewItem>(dto));
                    break;
                case MessageType.Changed:
                    AssembledComputerRules?.DoActionWithItem(x => x.Id == dto.Id, x => Mapper.Map(dto, x));
                    break;
            }
        }
    }
}