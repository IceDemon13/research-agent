using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Discussions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Discussions;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Discussions
{
    public sealed class DiscussionsViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly IMapper _mapper;

        public DiscussionsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;

            Filter = new DiscussionsFilterViewModel();

            RefreshCommand = new AsyncCommand(RefreshAsync);

            EditCommand = new DelegateCommand(Edit, () => SelectedDiscussion is not null);
            CreateCommand = new DelegateCommand(Create);
            CancelFilteringCommand = new DelegateCommand(Filter.Reset);
            messenger.Register<EntityMessage<DiscussionDto>>(this, OnDiscussionMessage);
        }

        public DiscussionsFilterViewModel Filter { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand CreateCommand { get; }

        public IDelegateCommand CancelFilteringCommand { get; }

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

        public bool AccessAllDiscussions
        {
            get { return GetProperty(() => AccessAllDiscussions); }
            set { SetProperty(() => AccessAllDiscussions, value); }
        }

        public ObservableCollection<DiscussionViewItem> Discussions
        {
            get { return GetProperty(() => Discussions); }
            set { SetProperty(() => Discussions, value); }
        }

        public DiscussionViewItem SelectedDiscussion
        {
            get { return GetProperty(() => SelectedDiscussion); }
            set { SetProperty(() => SelectedDiscussion, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Hashtags
        {
            get { return GetProperty(() => Hashtags); }
            private set { SetProperty(() => Hashtags, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Entities
        {
            get { return GetProperty(() => Entities); }
            private set { SetProperty(() => Entities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

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
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            (List<DiscussionStateDto> states, List<DiscussionHashtagDto>
                hashtags, PagedResult<EmployeeDto> employees, List<DiscussionTypeDto> types) result = await TaskExt.WhenAll(
                    WebClient.ExecuteApiRequestAsync(new QueryDiscussionStates()),
                    WebClient.ExecuteApiRequestAsync(new QueryDiscussionHashtags()),
                    WebClient.ExecuteApiRequestAsync(new QueryEmployees()),
                    WebClient.ExecuteApiRequestAsync(new QueryDiscussionTypes()));

            States = result.states
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Hashtags = result.hashtags
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Types = result.types
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Priorities = Dictionaries.GetItems<Priority>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.Id)
                .ToReadOnlyObservableCollection();

            AllEmployees = result.employees.Data
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            Entities = Dictionaries.GetItems<Entity>()
                .Select(x => new ComboBoxItem(x.Id, x.DisplayName))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            AccessAllDiscussions = WebClient.IsOperationAllowed(BusinessOperation.AccessAllDiscussions);

            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            PagedResult<DiscussionDto> discussions = await WebClient.ExecuteApiRequestAsync(new QueryDiscussions(Filter.GetFilteringItem()));

            Discussions = discussions.Data
                .Select(x => _mapper.Map<DiscussionViewItem>(x))
                .ToObservableCollection();
        }

        private void Create()
        {
            SizeableDialogDocumentManagerService.ShowView<DiscussionViewModel>(
                new DiscussionParameter(0, 0, 0, true),
                this);
        }

        private void Edit()
        {
            SizeableDialogDocumentManagerService.ShowView<DiscussionViewModel>(
                new DiscussionParameter(SelectedDiscussion.Id, 0, 0),
                this);
        }

        private void OnDiscussionMessage(EntityMessage<DiscussionDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Changed:
                    Discussions.DoActionWithItem(
                        x => x.Id == message.Entity.Id,
                        viewItem =>
                        {
                            viewItem = _mapper.Map(message.Entity, viewItem);

                            viewItem.HashtagIds = new ObservableCollection<int>(message.Entity.HashtagIds);
                        });
                    break;
                case MessageType.Added:
                    Discussions.Insert(0, _mapper.Map<DiscussionViewItem>(message.Entity));
                    break;
            }
        }
    }
}