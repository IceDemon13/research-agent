using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Helpers;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Discussions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.TransferObjects.Discussions;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Discussions
{
    public sealed class DiscussionViewModel : TelemartEditorViewModelBase<DiscussionDto, DiscussionParameter, DiscussionViewItem>
    {
        private readonly IViewModelResolver _viewModelResolver;
        private readonly ITelemartClientLogger _telemartClientLogger;
        private IReadOnlyCollection<DiscussionTypeDto> _allDiscussionTypes;
        private IReadOnlyCollection<DiscussionTypePropertyItem> _propertyItems;

        public DiscussionViewModel(
            IWebClient webClient,
            IViewModelResolver viewModelResolver,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            ITelemartClientLogger telemartClientLogger,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            _viewModelResolver = viewModelResolver;
            _telemartClientLogger = telemartClientLogger;
            OpenBitrixCommand = new DelegateCommand(OpenBitrix, () => Model?.BitrixId is not null);
            OpenDocumentCommand = new DelegateCommand(OpenDocument, () => SelectedEntityDocument is not null);
            CreateDocumentCommand = new DelegateCommand(CreateDocument, () => AllowEdit);
            DeleteDocumentCommand = new DelegateCommand(DeleteDocument, () => SelectedEntityDocument is not null && AllowEdit);
            OpenTypePropertiesCommand = new DelegateCommand(OpenTypeProperties, () => Model?.TypeId is not null && AllowEditBitrixProperties);
        }

        public IDelegateCommand OpenBitrixCommand { get; }

        public IDelegateCommand OpenDocumentCommand { get; }

        public IDelegateCommand CreateDocumentCommand { get; }

        public IDelegateCommand DeleteDocumentCommand { get; }

        public IDelegateCommand OpenTypePropertiesCommand { get; }

        #region DialogSettings

        public override int Width => 840;

        public override int MinWidth => 900;

        public override int MaxWidth => 1000;

        public override int Height => 460;

        public override int MinHeight => 460;

        public override int MaxHeight => 460;

        #endregion

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

        public ReadOnlyObservableCollection<ComboBoxItem> ActiveEmployees
        {
            get { return GetProperty(() => ActiveEmployees); }
            private set { SetProperty(() => ActiveEmployees, value); }
        }

        public ObservableCollection<DepartmentEmployeeViewItem> DepartmentEmployees
        {
            get { return GetProperty(() => DepartmentEmployees); }
            private set { SetProperty(() => DepartmentEmployees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Entities
        {
            get { return GetProperty(() => Entities); }
            private set { SetProperty(() => Entities, value); }
        }

        public ReadOnlyObservableCollection<DiscussionTypeDto> DiscussionTypes
        {
            get { return GetProperty(() => DiscussionTypes); }
            private set { SetProperty(() => DiscussionTypes, value); }
        }

        public DiscussionEntityDocumentViewItem SelectedEntityDocument
        {
            get { return GetProperty(() => SelectedEntityDocument); }
            set { SetProperty(() => SelectedEntityDocument, value); }
        }

        public bool ShowAllDisscutionTypes
        {
            get { return GetProperty(() => ShowAllDisscutionTypes); }
            set { SetProperty(() => ShowAllDisscutionTypes, value); }
        }

        public bool AllowEditBitrixProperties => IsNew && AllowEdit;

        public bool AllowEdit => Model != null && (IsNew || Model.CreatedBy == WebClient.AuthenticatedEmployee.Id || WebClient.IsOperationAllowed(BusinessOperation.AccessAllDiscussions));

        protected override string CreatedActionMessage { get; } = "создано";

        protected override string EntityName { get; } = "Обсуждение";

        protected override string UpdatedActionMessage { get; } = "сохранено";

        protected override async Task HandleLoadedAsync()
        {
            (List<DiscussionStateDto> states,
                List<DiscussionHashtagDto> hashtags,
                PagedResult<EmployeeDto> employees,
                List<DiscussionTypeDto> discussionTypes,
                List<DepartmentDto> departments) result = await TaskExt.WhenAll(
                WebClient.ExecuteApiRequestAsync(new QueryDiscussionStates()),
                WebClient.ExecuteApiRequestAsync(new QueryDiscussionHashtags()),
                WebClient.ExecuteApiRequestAsync(new QueryEmployees()),
                WebClient.ExecuteApiRequestAsync(new QueryDiscussionTypes()),
                WebClient.ExecuteApiRequestAsync(new QueryDepartments()));

            States = result.states
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Hashtags = result.hashtags
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Priorities = Dictionaries.GetItems<Priority>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            ActiveEmployees = result.employees.Data
                .Where(x => x.Active && x.BitrixId.HasValue)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            Entities = Dictionaries
                .GetItems<Entity>()
                .Select(x => new ComboBoxItem(x.Id, x.DisplayName))
                .ToReadOnlyObservableCollection();

            _allDiscussionTypes = result.discussionTypes.ToReadOnlyObservableCollection();

            DepartmentEmployees = new ObservableCollection<DepartmentEmployeeViewItem>();

            DepartmentDto[] filteredDepartments = result.departments.Where(x => x.Active).ToArray();

            DepartmentEmployees.AddRange(filteredDepartments.Select(x => new DepartmentEmployeeViewItem()
            {
                DisplayName = x.Name,
                DepartmentId = x.Id,
                ParentDepartmentId = x.ParentId,
                EmployeeId = null
            }));

            int maxDepartmentId = filteredDepartments.Max(x => x.Id);

            DepartmentEmployees.AddRange(result.employees.Data
                .Where(x => x.Active && x.BitrixId.HasValue).Select(x => new DepartmentEmployeeViewItem()
                {
                    ParentDepartmentId = x.DepartmentId,
                    DepartmentId = ++maxDepartmentId,
                    EmployeeId = x.Id,
                    DisplayName = x.Name
                }));

            await base.HandleLoadedAsync();

            DiscussionParameter parameter = (DiscussionParameter)Parameter;

            ShowAllDisscutionTypes = parameter.ShowAllTypes;

            if (IsNew && parameter.EntityId > 0)
            {
                Model.EntityDocuments ??= new ObservableCollection<DiscussionEntityDocumentViewItem>();

                Model.EntityDocuments.Add(
                    new DiscussionEntityDocumentViewItem()
                    {
                        DocumentId = parameter.DocumentId,
                        EntityId = parameter.EntityId
                    });
            }

            bool hasTypeId = parameter.TypeId.HasValue && IsNew;

            OnEntityDocumentsChanged(hasTypeId);

            ModifyModelByType(parameter.TypeId);
        }

        protected override void AfterSetData()
        {
            RaisePropertiesChanged(nameof(AllowEdit), nameof(AllowEditBitrixProperties));

            base.AfterSetData();
        }

        protected override void OnModelPropertyChangedInternal(object sender, PropertyChangedEventArgs e)
        {
            base.OnModelPropertyChangedInternal(sender, e);

            RaisePropertiesChanged(nameof(AllowEdit), nameof(AllowEditBitrixProperties));

            switch (e.PropertyName)
            {
                case nameof(DiscussionViewItem.TypeId):

                    DiscussionTypeDto type = DiscussionTypes.FirstOrDefault(x => x.Id == Model.TypeId);

                    if (type is null)
                    {
                        Model.Title = null;
                        return;
                    }

                    if (DiscussionTypes.Any(x => x.ParentId == type.Id)
                        && !MessageFacadeService.Confirm("Детализация типа обсуждения может ускорить процесс. Вы уверены, что нет более конкретного типа?"))
                    {
                        return;
                    }

                    if (IsNew)
                    {
                        BuildDiscussionByType(type);
                    }

                    BuildTitle();

                    break;
            }
        }

        protected override async Task<Result<DiscussionDto>> CreateEntityAsync()
        {
            DiscussionCreateDto createDto = Mapper.Map<DiscussionViewItem, DiscussionCreateDto>(Model);

            Result<DiscussionDto> result = await WebClient.ExecuteApiRequestAsync(new CreateDiscussion(createDto));

            if (Model.ClientLogs && result.IsSuccess)
            {
                await _telemartClientLogger.SendLogsAsync(Model.Title, true, GetBitrixLink(result.Data.BitrixId));
            }

            return result;
        }

        protected override async Task<DiscussionDto> GetEntityAsync(int id)
        {
            return await WebClient.ExecuteApiRequestAsync(new QueryDiscussion(id));
        }

        protected override Task<LockResponse<DiscussionDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<DiscussionDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание обсуждения";
        }

        protected override void SetEditTitle()
        {
            Title = $"Обсуждение №{Model.Id}";
        }

        protected override Task<Result<DiscussionDto>> UpdateEntityAsync()
        {
            DiscussionUpdateDto updateDto = Mapper.Map<DiscussionViewItem, DiscussionUpdateDto>(Model);

            return WebClient.ExecuteApiRequestAsync(new UpdateDiscussion(Model.Id, updateDto));
        }

        private static string GetBitrixLink(string bitrixId)
        {
            return $"https://bitrix.telemart.ua/company/personal/user/0/tasks/task/view/{bitrixId}/";
        }

        private void OpenBitrix()
        {
            ProcessHelper.Start(GetBitrixLink(Model.BitrixId));
        }

        private void CreateDocument()
        {
            DiscussionEntityDocumentViewModel viewModel = DialogDocumentManagerService.ShowView<DiscussionEntityDocumentViewModel>(null, this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Model.EntityDocuments ??= new ObservableCollection<DiscussionEntityDocumentViewItem>();

            if (Model.EntityDocuments.Any(x => x.EntityId == viewModel.SelectedEntityId && x.DocumentId == viewModel.DocumentId))
            {
                MessageFacadeService.ShowNotificationWarning("Документ уже добавлен");
                return;
            }

            Model.EntityDocuments.Add(new DiscussionEntityDocumentViewItem()
            {
                EntityId = viewModel.SelectedEntityId!.Value,
                DocumentId = viewModel.DocumentId!.Value,
                DiscussionId = Model.Id
            });

            OnEntityDocumentsChanged(true);
        }

        private void DeleteDocument()
        {
            Model.EntityDocuments.Remove(SelectedEntityDocument);
            OnEntityDocumentsChanged(true);
        }

        private void OpenTypeProperties()
        {
            if (!IsNew)
            {
                return;
            }

            DiscussionTypePropertiesViewModel model = SizeableDialogDocumentManagerService.ShowView<DiscussionTypePropertiesViewModel>(new DiscussionTypeFillsParameter(Model?.TypeId, _propertyItems), this);

            if (model.IsOk)
            {
                Model!.Body = model.Body;
                Model!.FormatBody = model.FormatBody;
                _propertyItems = model.PropertyItems;
            }
        }

        private void OpenDocument()
        {
            (bool ViewModelSupport, object Message) result = _viewModelResolver.Resolve(SelectedEntityDocument.EntityId, SelectedEntityDocument.DocumentId);

            if (result.ViewModelSupport)
            {
                ViewModelOpenHelper.OpenByMessage(result.Message, Messenger, this);
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning("Документ не поддерживает открытие");
            }
        }

        private void OnEntityDocumentsChanged(bool rebuildTitle)
        {
            FilterDiscussionTypes();

            if (rebuildTitle)
            {
                BuildTitle();
            }
        }

        private void FilterDiscussionTypes()
        {
            int[] entityIds = Model.EntityDocuments?
                .Select(x => x.EntityId)
                .ToArray() ?? Array.Empty<int>();

            DiscussionTypes = ShowAllDisscutionTypes && Model.EntityDocuments?.Any() != true
                ? _allDiscussionTypes.ToReadOnlyObservableCollection()
                : _allDiscussionTypes
                    .Where(x => x.EntityIds?.Any() != true || entityIds.Intersect(x.EntityIds).Any() || x.Id == Model?.TypeId)
                    .ToReadOnlyObservableCollection();
        }

        private void ModifyModelByType(int? typeId)
        {
            if (typeId.HasValue)
            {
                Model.TypeId = typeId.Value;

                DiscussionTypeDto type = DiscussionTypes.FirstOrDefault(x => x.Id == Model.TypeId);

                BuildDiscussionByType(type);
            }
        }

        private void BuildTitle()
        {
            if (Model.TypeId is null)
            {
                return;
            }

            string newTitle;

            string discussionTypeName = _allDiscussionTypes.First(x => x.Id == Model.TypeId.Value).NameUkr;

            if (Model.EntityDocuments?.Any() == true)
            {
                newTitle = $"{discussionTypeName} ({string.Join(", ", Model.EntityDocuments.Select(x => $"{Entities.First(z => z.Id == x.EntityId)} №{x.DocumentId}"))})";
            }
            else
            {
                newTitle = discussionTypeName;
            }

            Model.Title = newTitle;
        }

        private void BuildDiscussionByType(DiscussionTypeDto type)
        {
            if (type.Template?.Parameters?.Deadline is not null)
            {
                Model.Deadline = DateTime.Now.Add(type.Template.Parameters.Deadline.Value);
            }

            Model.ExecutorEmployeeId = type.Template?.Parameters?.ExecutorEmployeeId;
            Model.PriorityBitrix = type.PriorityBitrix;
            Model.UseBitrixGroupFromExecutorDepartment = type.Template?.Parameters?.UseGroupFromExecutorDepartment ?? false;
            Model.TaskControlBitrix = type.Template?.Parameters?.TaskControlBitrix ?? false;
            Model.PriorityId = type.PriorityId;
            Model.HashtagIds = type.Template?.Parameters?.HashtagIds?.ToObservableCollection() ?? new ObservableCollection<int>();
            Model.CoExecutorEmployeeIds = type.Template?.Parameters?.CoExecutorEmployeeIds?.ToObservableCollection() ?? new ObservableCollection<int>();
            Model.AuditorEmployeeIds = type.Template?.Parameters?.AuditorEmployeeIds?.ToObservableCollection() ?? new ObservableCollection<int>();
            Model.Title = type.NameUkr;
            Model.ClientLogs = type.Template?.Parameters?.ClientLogs ?? false;
        }
    }
}