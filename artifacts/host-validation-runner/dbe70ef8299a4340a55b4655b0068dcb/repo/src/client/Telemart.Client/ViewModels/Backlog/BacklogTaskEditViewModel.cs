using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Backlog;
using Telemart.Client.Data.Requests.Features.Backlog.Actions;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class BacklogTaskEditViewModel : TelemartEditorViewModelBase<BacklogTaskDto, BacklogTaskViewMessage, BacklogTaskViewItem>
    {
        private bool editAllowed;
        private IReadOnlyDictionary<int, EmployeeDto> _employees;
        private IReadOnlyDictionary<int, string> _departmentNames;
        private IReadOnlyCollection<BacklogCategoryDto> _categories;

        public BacklogTaskEditViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            DocumentCommands documentCommands,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            DocumentCommands = documentCommands;

            AddOrRemoveTaskFromFavoritesCommand = new AsyncCommand(AddOrRemoveTaskFromFavoritesAsync);
            ChangeEstimateCommand = new DelegateCommand(ChangeEstimate, CanChangeEstimate);
            ChangeResponsibleCommand = new DelegateCommand(ChangeResponsible, CanChangeResponsible);

            Messenger.Register<BacklogTaskMessage>(this, OnBacklogTaskMessage);
        }

        public BacklogTaskEditViewModel()
        {
        }

        public event Action OnEmployeeLocked;

        public event Action OnEmployeeUnlocked;

        #region Commands

        public IAsyncCommand AddOrRemoveTaskFromFavoritesCommand { get; }

        public IDelegateCommand ChangeEstimateCommand { get; }

        public IDelegateCommand ChangeResponsibleCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        #endregion

        #region INPC

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ReadOnlyObservableCollection<BacklogTaskState> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public ReadOnlyObservableCollection<Priority> Priorities
        {
            get { return GetProperty(() => Priorities); }
            private set { SetProperty(() => Priorities, value); }
        }

        public ReadOnlyObservableCollection<BacklogTaskResolution> Resolutions
        {
            get { return GetProperty(() => Resolutions); }
            private set { SetProperty(() => Resolutions, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ObservableCollection<BacklogCategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public bool? IsNotAnyCategoryChecked => Categories?.All(x => x.Checked == false);

        #endregion

        protected override string CreatedActionMessage => "создана";

        protected override string EntityName => "Задача";

        protected override string UpdatedActionMessage => "сохранена";

        protected override Task<Result<BacklogTaskDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(BacklogTaskDto taskDto, MessageType messageType)
        {
            return new BacklogTaskMessage(taskDto, messageType);
        }

        protected override Task<BacklogTaskDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryBacklogTask(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            editAllowed = WebClient.IsOperationAllowed(BusinessOperation.BacklogTaskUpdate);

            BacklogTaskViewMessage parameter = (BacklogTaskViewMessage)Parameter;

            if (parameter.IsNew)
            {
                throw new NotSupportedException("Task creation is not supported");
            }

            States = Dictionaries.GetItems<BacklogTaskState>().ToReadOnlyObservableCollection();
            Priorities = Dictionaries.GetItems<Priority>().ToReadOnlyObservableCollection();
            Resolutions = Dictionaries.GetItems<BacklogTaskResolution>().ToReadOnlyObservableCollection();

            await Task.WhenAll(RefreshEmployeesAsync(), DocumentCommands.InitAsync(this, DialogDocumentManagerService), RefreshCategoriesAsync(), RefreshDepartmentsAsync());

            await base.HandleLoadedAsync();

            Categories.ForEach(x => x.PropertyChanged -= OnCategoryPropertyChanged);

            Categories = _categories
                .Where(x => x.Active || Model.CategoryIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => Mapper.Map<BacklogCategoryViewItem>(x))
                .ToObservableCollection();

            Categories.CheckItems(Model.CategoryIds);

            Categories.ForEach(x => x.PropertyChanged += OnCategoryPropertyChanged);

            RefreshSummaryItems();
        }

        protected override void AfterSetData()
        {
            if (IsLockedByCurrentEmployee)
            {
                OnEmployeeLocked?.Invoke();
            }
            else
            {
                OnEmployeeUnlocked?.Invoke();
            }

            if (Model.EmployeeId.HasValue)
            {
                EmployeeDto employeeDto = _employees[Model.EmployeeId.Value];

                Model.Employee = new ComboBoxItem(employeeDto.Id, employeeDto.Name);
            }
            else
            {
                Model.Employee = null;
            }

            if (ModelOriginal.EmployeeId.HasValue)
            {
                EmployeeDto employeeDto = _employees[ModelOriginal.EmployeeId.Value];

                ModelOriginal.Employee = new ComboBoxItem(employeeDto.Id, employeeDto.Name);
            }
            else
            {
                ModelOriginal.Employee = null;
            }

            Model.IsFavorite = Model.FavoriteEmployeeIds?.Any(x => x == WebClient.AuthenticatedEmployee.Id) == true;
            ModelOriginal.IsFavorite = ModelOriginal.FavoriteEmployeeIds?.Any(x => x == WebClient.AuthenticatedEmployee.Id) == true;

            Categories?.CheckItems(Model.CategoryIds);

            RefreshSummaryItems();
        }

        protected override Task<LockResponse<BacklogTaskDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockBacklogTask(id, checkPermissions: true));
        }

        protected override Task<LockResponse<BacklogTaskDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockBacklogTask(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание Задачи";
        }

        protected override void SetEditTitle()
        {
            Title = $"Задача №{Model.Id}";
        }

        protected override Task<Result<BacklogTaskDto>> UpdateEntityAsync()
        {
            BacklogTaskSaveDto saveDto = Mapper.Map<BacklogTaskSaveDto>(Model);
            UpdateBacklogTask gatewayRequest = new UpdateBacklogTask(Model.Id, saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override bool CanEdit()
        {
            return editAllowed && Model?.State.Id != BacklogTaskState.Canceled.Id && Model?.State.Id != BacklogTaskState.Completed.Id;
        }

        protected override void Close()
        {
            Categories.ForEach(x => x.PropertyChanged -= OnCategoryPropertyChanged);
            base.Close();
        }

        protected override void OnInitializeInDesignModeInternal()
        {
            States = new ReadOnlyObservableCollection<BacklogTaskState>(new ObservableCollection<BacklogTaskState> { BacklogTaskState.Idea });
            Resolutions = new ReadOnlyObservableCollection<BacklogTaskResolution>(new ObservableCollection<BacklogTaskResolution> { BacklogTaskResolution.Completed });

            Model.Id = 23;
            Model.AuthorId = 8;
            Model.BitrixId = 37;
            Model.State = BacklogTaskState.Idea;
            Model.Resolution = null;
            Model.Priority = Priority.Low;
            Model.Name = "Show task in design view";
            Model.Estimate = 8;
            Model.FavoriteEmployeeIds = Array.Empty<int>();
            Model.IsFavorite = false;
            Model.Comment = "Some long text";
            Model.CreatedBy = 8;
            Model.CreatedOn = DateTime.Now.AddDays(-1);
            Model.ModifiedBy = 8;
            Model.ModifiedOn = DateTime.Now;

            RefreshSummaryItems();
        }

        private async Task RefreshEmployeesAsync()
        {
            PagedResult<EmployeeDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            _employees = pagedResult.Data.ToDictionary(x => x.Id);

            Employees = pagedResult.Data
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshCategoriesAsync()
        {
            _categories = await WebClient.ExecuteApiRequestAsync(new QueryBacklogCategories(), true);
        }

        private async Task RefreshDepartmentsAsync()
        {
            List<DepartmentDto> departments = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            _departmentNames = departments.ToDictionary(x => x.Id, x => x.Name);
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            yield return new SummaryViewItem("Заказчик", $"{_employees.GetValueOrDefault(Model.AuthorId)?.Name}");
            yield return new SummaryViewItem("Создал", $"{_employees.GetValueOrDefault(Model.CreatedBy)?.Name} ({Model.CreatedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            yield return new SummaryViewItem("Изменил", $"{_employees.GetValueOrDefault(Model.ModifiedBy)?.Name} ({Model.ModifiedOn.ToString(DateFormattingRules.FullDateTimeFormat)})");
            yield return new SummaryViewItem("Битрикс", Model.BitrixId.ToString());
            yield return new SummaryViewItem("Статус", Model.State.Name);
            yield return new SummaryViewItem("Отдел", _departmentNames.GetValueOrDefault(_employees.GetValueOrDefault(Model.CreatedBy)?.DepartmentId ?? 0));
        }

        private bool CanChangeEstimate()
        {
            return Model != null && Model.EmployeeLockId == null && Model.State.CanChangeEstimate;
        }

        private void ChangeEstimate()
        {
            ExecuteLockableOperationAsync(lockedEntity =>
            {
                DialogDocumentManagerService.ShowView<ChangeEstimateViewModel>(lockedEntity, this);
            });
        }

        private bool CanChangeResponsible()
        {
            return Model != null && Model.EmployeeLockId == null && Model.State.CanChangeResponsible;
        }

        private void ChangeResponsible()
        {
            ExecuteLockableOperationAsync(lockedEntity =>
            {
                DialogDocumentManagerService.ShowView<ChangeResponsibleViewModel>(lockedEntity, this);
            });
        }

        private Task AddOrRemoveTaskFromFavoritesAsync()
        {
            Task task = Task.CompletedTask;

            if (!Model.IsFavorite)
            {
                if (MessageFacadeService.Confirm("Удалить задачу из избранного?"))
                {
                    task = RemoveTaskFromFavoritesAsync();
                }
                else
                {
                    Model.IsFavorite = true;
                }
            }
            else
            {
                if (MessageFacadeService.Confirm("Добавить задачу в избранное?"))
                {
                    task = AddTaskToFavoritesAsync();
                }
                else
                {
                    Model.IsFavorite = false;
                }
            }

            return task;
        }

        private async Task AddTaskToFavoritesAsync()
        {
            try
            {
                Result<BacklogTaskDto> result = await WebClient.ExecuteApiRequestAsync(new AddBacklogTaskToFavorites(Model.Id));

                SetData(result.Data);

                Messenger.Send(new BacklogTaskMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Задача успешно добавлена в избранное");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении задачи в избранное");
                ShowValidationResultView("Ошибки при добавлении задачи в избранное", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to add task to favorites");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при добавлении задачи в избранное");
                Logger.LogError(exception, "Error while adding task to favorites");
            }
        }

        private async Task RemoveTaskFromFavoritesAsync()
        {
            try
            {
                Result<BacklogTaskDto> result = await WebClient.ExecuteApiRequestAsync(new RemoveBacklogTaskFromFavorites(Model.Id));

                SetData(result.Data);

                Messenger.Send(new BacklogTaskMessage(result.Data, MessageType.Changed));
                MessageFacadeService.ShowNotificationInfo("Задача успешно удалена из избранного");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении задачи из избранного");
                ShowValidationResultView("Ошибки при удалении задачи из избранного", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to remove backlog task from favorites");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении задачи из избранного");
                Logger.LogError(exception, "Error while removing backlog task from favorites");
            }
        }

        private void OnCategoryPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Model.CategoryIds = Categories.Where(x => x.Checked != false).Select(x => x.Id).OrderBy(x => x).ToArray();

            RaisePropertyChanged(nameof(IsNotAnyCategoryChecked));
        }

        private void OnBacklogTaskMessage(BacklogTaskMessage message)
        {
            if (message.Entity.Id == Model.Id)
            {
                SetData(message.Entity);
                RefreshSummaryItems();
            }
        }
    }
}