using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.RobotProperty;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.RobotProperty;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Dialogs;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.RobotProperties
{
    public class RobotPropertyViewModel : TelemartEditorViewModelBase<RobotPropertyDto, RobotPropertyParameter, RobotPropertyViewItem>
    {
        public RobotPropertyViewModel(
            IWebClient webClient,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper,
            IDictionaries dictionaries)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            CreateGroupNameCommand = new DelegateCommand(CreateGroupName);
            AddValueCommand = new DelegateCommand(AddValue);
            DeleteValueCommand = new DelegateCommand(DeleteValue, () => SelectedValue is not null);

            Types = dictionaries.GetItems<RobotPropertyType>().ToReadOnlyObservableCollection();
        }

        public IDelegateCommand DeleteValueCommand { get; }

        public IDelegateCommand AddValueCommand { get; }

        public IDelegateCommand CreateGroupNameCommand { get; }

        public ReadOnlyObservableCollection<RobotPropertyType> Types
        {
            get { return GetProperty(() => Types); }
            set { SetProperty(() => Types, value); }
        }

        public ObservableCollection<string> GroupNames
        {
            get { return GetProperty(() => GroupNames); }
            set { SetProperty(() => GroupNames, value); }
        }

        public RobotPropertyValueViewItem SelectedValue
        {
            get { return GetProperty(() => SelectedValue); }
            set { SetProperty(() => SelectedValue, value); }
        }

        protected override string CreatedActionMessage { get; } = "Переменная создана";

        protected override string EntityName { get; } = "Переменная робота";

        protected override string UpdatedActionMessage { get; } = "сохранена";

        protected override Task<Result<RobotPropertyDto>> CreateEntityAsync()
        {
            RobotPropertyDto createDto = Mapper.Map<RobotPropertyDto>(Model);

            return WebClient.ExecuteApiRequestAsync(new CreateRobotProperty(createDto));
        }

        protected override Task<RobotPropertyDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryRobotProperty(id));
        }

        protected override Task<LockResponse<RobotPropertyDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<RobotPropertyDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание переменной робота";
        }

        protected override void SetEditTitle()
        {
            Title = "Изменение переменной робота";
        }

        protected override Task HandleLoadedAsync()
        {
            RobotPropertyParameter parameter = (RobotPropertyParameter)Parameter;

            GroupNames = parameter.GroupNames.ToObservableCollection();

            return base.HandleLoadedAsync();
        }

        protected override Task<Result<RobotPropertyDto>> UpdateEntityAsync()
        {
            RobotPropertyDto updateDto = Mapper.Map<RobotPropertyDto>(Model);

            return WebClient.ExecuteApiRequestAsync(new UpdateRobotProperty(Model.Id, updateDto));
        }

        protected override bool IsValid(RobotPropertyViewItem model)
        {
            if (Model.Values != null && Model.Values.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                MessageFacadeService.ShowNotificationError("Сохранение с ошибками запрещено");
                return false;
            }

            return true;
        }

        private void CreateGroupName()
        {
            GetTextFromUserParameter fromUserParameter = new GetTextFromUserParameter(
                "Название",
                "Создание названия группы",
                null,
                "Не валидное значение. ");

            GetTextFromUserViewModel fromUserViewModel = DialogDocumentManagerService.ShowView<GetTextFromUserViewModel>(fromUserParameter, this);

            if (!fromUserViewModel.IsOk)
            {
                return;
            }

            if (fromUserViewModel.Content.Length > 50)
            {
                MessageFacadeService.ShowNotificationError("Максимально допустимая длина 50 символов");
                return;
            }

            string groupName = fromUserViewModel.Content.Trim();

            if (!GroupNames.Contains(groupName))
            {
                GroupNames.Add(groupName);
            }

            GroupNames = GroupNames.OrderBy(x => x).ToObservableCollection();
            Model.GroupName = groupName;
        }

        private void DeleteValue()
        {
            Model.Values.Remove(SelectedValue);
        }

        private void AddValue()
        {
            Model.Values ??= new ObservableCollection<RobotPropertyValueViewItem>();
            Model.Values.Insert(0, new RobotPropertyValueViewItem(0, Model.Id, null, null));
        }
    }
}