using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.RobotProperty;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects.RobotProperty;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class RobotPropertiesViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly IMapper _mapper;
        private readonly IErrorHandler _errorHandler;

        public RobotPropertiesViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessenger messenger,
            IMapper mapper,
            IErrorHandler errorHandler,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _mapper = mapper;
            _errorHandler = errorHandler;

            AddCommand = new DelegateCommand(Add);
            EditCommand = new DelegateCommand(Edit, () => SelectedRobotProperty != null);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            DeleteCommand = new AsyncCommand(DeleteAsync, () => SelectedRobotProperty is not null);

            messenger.Register<EntityMessage<RobotPropertyDto>>(this, OnRobotPropertyMessage);

            Types = dictionaries.GetItems<RobotPropertyType>().ToReadOnlyObservableCollection();
        }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand DeleteCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand AddCommand { get; }

        public ObservableCollection<RobotPropertyViewItem> Properties
        {
            get { return GetProperty(() => Properties); }
            private set { SetProperty(() => Properties, value); }
        }

        public RobotPropertyViewItem SelectedRobotProperty
        {
            get { return GetProperty(() => SelectedRobotProperty); }
            set { SetProperty(() => SelectedRobotProperty, value); }
        }

        public ReadOnlyObservableCollection<RobotPropertyType> Types
        {
            get { return GetProperty(() => Types); }
            set { SetProperty(() => Types, value); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage hotkeyMessage)
        {
            bool handled = false;

            switch (hotkeyMessage.HotkeyMessageType)
            {
                case HotkeyMessageType.Refresh:
                    RefreshCommand.Execute(null);
                    handled = true;
                    break;

                case HotkeyMessageType.Delete:
                    DeleteCommand.Execute(null);
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

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            await RefreshAsync();

            await base.HandleLoadedAsync();
        }

        private async Task RefreshAsync()
        {
            List<RobotPropertyDto> properties = await WebClient.ExecuteApiRequestAsync(new QueryRobotProperties());

            Properties = properties
                .Select(x => _mapper.Map<RobotPropertyViewItem>(x))
                .ToObservableCollection();
        }

        private void Add()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.RobotPropertiesAllowEdit))
            {
                MessageFacadeService.ShowNotificationError("У Вас нет прав на выполнение операции");
                return;
            }

            DialogDocumentManagerService.ShowView<RobotPropertyViewModel>(new RobotPropertyParameter(0, GetGroupNames()), this);
        }

        private void Edit()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.RobotPropertiesAllowEdit))
            {
                MessageFacadeService.ShowNotificationError("У Вас нет прав на выполнение операции");
                return;
            }

            DialogDocumentManagerService.ShowView<RobotPropertyViewModel>(new RobotPropertyParameter(SelectedRobotProperty.Id, GetGroupNames()), this);
        }

        private void OnRobotPropertyMessage(EntityMessage<RobotPropertyDto> message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:

                    RobotPropertyViewItem robotProperty = _mapper.Map<RobotPropertyViewItem>(message.Entity);

                    Properties.Add(robotProperty);

                    break;
                case MessageType.Changed:
                    RobotPropertyViewItem editedRobotProperty = Properties.First(x => x.Id == message.Entity.Id);

                    _mapper.Map(message.Entity, editedRobotProperty);

                    break;
            }
        }

        private IReadOnlyCollection<string> GetGroupNames()
        {
            return Properties.GroupBy(x => x.GroupName).Select(x => x.Key).ToArray();
        }

        private async Task DeleteAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            (await _errorHandler.HandleErrorsAsync(_ => WebClient.ExecuteApiRequestAsync(new DeleteRobotProperty(SelectedRobotProperty.Id)), "удалении свойства", "Свойство удалено", this, true))
                .IfNotNull(_ =>
                {
                    Properties.Remove(SelectedRobotProperty);
                });
        }
    }
}