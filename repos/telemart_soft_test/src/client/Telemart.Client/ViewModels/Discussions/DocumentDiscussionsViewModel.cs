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
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Discussions;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Discussions
{
    public sealed class DocumentDiscussionsViewModel : TelemartDialogViewModelBase
    {
        private readonly IMapper _mapper;
        private DocumentDiscussionsParameter _parameter;

        public DocumentDiscussionsViewModel(
            IWebClient webClient,
            IMapper mapper,
            IMessenger messenger,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            CreateCommand = new DelegateCommand(Create);
            EditCommand = new DelegateCommand(Edit, () => SelectedDiscussion is not null);
            messenger.Register<EntityMessage<DiscussionDto>>(this, OnDiscussionMessage);
        }

        public IDelegateCommand CreateCommand { get; }

        public IDelegateCommand EditCommand { get; }

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

        public ReadOnlyObservableCollection<ComboBoxItem> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ActiveEmployees
        {
            get { return GetProperty(() => ActiveEmployees); }
            private set { SetProperty(() => ActiveEmployees, value); }
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

        protected override async Task HandleLoadedAsync()
        {
            _parameter = (DocumentDiscussionsParameter)Parameter;

            DiscussionsFilteringItem filteringItem = new DiscussionsFilteringItem()
            {
                EntityId = _parameter.EntityId,
                DocumentId = _parameter.DocumentId
            };

            (PagedResult<DiscussionDto> discussions,
                List<DiscussionStateDto> states,
                List<DiscussionHashtagDto> hashtags,
                PagedResult<EmployeeDto> employees) result = await TaskExt.WhenAll(
                    WebClient.ExecuteApiRequestAsync(new QueryDiscussions(filteringItem)),
                    WebClient.ExecuteApiRequestAsync(new QueryDiscussionStates()),
                    WebClient.ExecuteApiRequestAsync(new QueryDiscussionHashtags()),
                    WebClient.ExecuteApiRequestAsync(new QueryEmployees()));

            Discussions = result.discussions.Data
                .Select(x => _mapper.Map<DiscussionViewItem>(x))
                .ToObservableRangeCollection();

            States = result.states
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Hashtags = result.hashtags
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Priorities = Dictionaries.GetItems<Priority>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            AllEmployees = result.employees.Data
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            ActiveEmployees = result.employees.Data
                .Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            Entity entity = Dictionaries.GetItemById<Entity>(_parameter.EntityId);

            Title = $"Обсуждения по документу: '{entity.DisplayName} №{_parameter.DocumentId}'";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private void Create()
        {
            SizeableDialogDocumentManagerService.ShowView<DiscussionViewModel>(
                new DiscussionParameter(0, _parameter.EntityId, _parameter.DocumentId),
                this);
        }

        private void Edit()
        {
            SizeableDialogDocumentManagerService.ShowView<DiscussionViewModel>(
                new DiscussionParameter(SelectedDiscussion.Id, _parameter.EntityId, _parameter.DocumentId),
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