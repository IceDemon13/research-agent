using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.RobotProperties;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Category
{
    internal sealed class DirectoryCategoriesViewModel : ViewModelBase, IDocumentContent
    {
        private CategoryOptionsViewModel emptyCategoryOptions;

        public DirectoryCategoriesViewModel(
             IWebClient webClient,
             IDictionaries dictionaries,
             IMessageFacadeService messageFacadeService,
             IMapper mapper,
             IMessenger messenger,
             ICategoryOptionsInitializer categoryOptionsInitializer,
             ILogger<DirectoryCategoriesViewModel> logger)
        {
            WebClient = webClient ?? throw new ArgumentNullException(nameof(webClient));
            Dictionaries = dictionaries ?? throw new ArgumentNullException(nameof(dictionaries));
            MessageFacadeService = messageFacadeService ?? throw new ArgumentNullException(nameof(messageFacadeService));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            CategoryOptionsInitializer = categoryOptionsInitializer ?? throw new ArgumentNullException(nameof(categoryOptionsInitializer));
            Logger = logger;

            HandleLoadedCommand = new DelegateCommand(HandleLoaded);
            ShowCreateCategoryDialogCommand = new DelegateCommand<CategoryViewItem>(ShowCreateCategoryDialog, CanShowCreateCategoryDialog);
            RefreshCommand = new AsyncCommand(RefreshAsync);
            ShowSaveOptionsDialogCommand = new DelegateCommand(ShowSaveOptionsDialog);
            SetCategoryCommand = new AsyncCommand<CategoryViewItem>(SetCategoryAsync);
            LockCommand = new AsyncCommand(LockAsync, () => SelectedCategory != null);
            CancelCommand = new AsyncCommand(CancelCommandAsync);
            ReorderCommand = new DelegateCommand<CategoryViewItem>(Reorder, CanReorder);
            MoveCommand = new DelegateCommand<CategoryViewItem>(Move, CanMove);
            RobotCommand = new DelegateCommand(Robot, () => SelectedCategory != null);

            Messenger.Register<CategoryMessage>(this, OnCategoryMessage);
        }

        public DirectoryCategoriesViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandleLoadedCommand { get; }

        public IAsyncCommand RefreshCommand { get; }

        public IAsyncCommand SetCategoryCommand { get; }

        public IAsyncCommand LockCommand { get; }

        public IAsyncCommand CancelCommand { get; }

        public IDelegateCommand ShowCreateCategoryDialogCommand { get; }

        public IDelegateCommand ShowSaveOptionsDialogCommand { get; }

        public IDelegateCommand ReorderCommand { get; }

        public IDelegateCommand MoveCommand { get; }

        public IDelegateCommand RobotCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public CategoryOptionsViewModel CategoryOptions
        {
            get { return GetProperty(() => CategoryOptions); }
            set { SetProperty(() => CategoryOptions, value); }
        }

        public CategoryOptionsViewModel EmptyCategoryOptions => emptyCategoryOptions ?? (emptyCategoryOptions = CategoryOptionsViewModel.GetEmptyViewModel());

        public CategoryViewItem SelectedCategory
        {
            get { return GetProperty(() => SelectedCategory); }
            set { SetProperty(() => SelectedCategory, value); }
        }

        public bool IsLockedByCurrentEmployee
        {
            get { return GetProperty(() => IsLockedByCurrentEmployee); }
            set { SetProperty(() => IsLockedByCurrentEmployee, value); }
        }

        public bool IsLongOperationInProgress
        {
            get
            {
                return GetProperty(() => IsLongOperationInProgress);
            }

            set
            {
                SetProperty(() => IsLongOperationInProgress, value, () =>
                {
                    RaisePropertyChanged(nameof(IsLongOperationInProgress));
                    if (CategoryOptions != null)
                    {
                        CategoryOptions.IsLongOperationInProgress = value;
                    }
                });
            }
        }

        #endregion

        public bool IsEditingAllowed => IsAdmin || IsMarketer || IsProduct;

        public bool IsMarketer => CurrentEmployee.HasAnyRole(Role.Marketer);

        public bool IsProduct => CurrentEmployee.HasAnyRole(Role.Product);

        public bool IsAdmin => CurrentEmployee.HasAnyRole(Role.Admin);

        public EmployeeDto CurrentEmployee => WebClient.AuthenticatedEmployee;

        public IDocumentOwner DocumentOwner { get; set; }

        public object Title { get; }

        private ILogger<DirectoryCategoriesViewModel> Logger { get; }

        private IMessageFacadeService MessageFacadeService { get; }

        private IWebClient WebClient { get; }

        private IDictionaries Dictionaries { get; }

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        private ICategoryOptionsInitializer CategoryOptionsInitializer { get; }

        private IDocumentManagerService CreateCategoryDocumentManagerService => GetService<IDocumentManagerService>("CreateCategoryDocumentManagerService");

        private IDocumentManagerService SaveCategoryDocumentManagerService => GetService<IDocumentManagerService>("SaveCategoryDocumentManagerService");

        private IDocumentManagerService DialogDocumentManagerService => GetService<IDocumentManagerService>("DialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NonModalSizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService");

        public void OnClose(CancelEventArgs e)
        {
        }

        public void OnDestroy()
        {
            if (IsLockedByCurrentEmployee)
            {
                CancelCommand.Execute(null);
            }
        }

        private static string GetFullName(int categoryId, IEnumerable<CategoryViewItem> categories, string separator = "/")
        {
            Dictionary<int, CategoryViewItem> categoriesDictionary = categories.ToDictionary(x => x.Id);

            Stack<string> names = new Stack<string>();

            int id = categoryId;

            while (categoriesDictionary.TryGetValue(id, out CategoryViewItem currentCategory))
            {
                names.Push(currentCategory.Name);
                id = currentCategory.ParentId;
            }

            return string.Join(separator, names);
        }

        private void HandleLoaded()
        {
            if (Categories != null)
            {
                return;
            }

            CategoryOptionsInitializer.InitializeCategoryOptions(EmptyCategoryOptions);
            RefreshCommand.Execute(null);
        }

        private async Task RefreshAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                int? categoryId = SelectedCategory?.Id;

                CategoryOptions = EmptyCategoryOptions;
                Categories = null;
                SelectedCategory = null;

                List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories()).GetPagedResultDataAsync();

                Categories = categories.Select(x => Mapper.Map<CategoryViewItem>(x)).ToObservableCollection();

                if (categoryId.HasValue)
                {
                    SelectedCategory = Categories.FirstOrDefault(x => x.Id == categoryId.Value);

                    if (SelectedCategory != null)
                    {
                        CategoryOptionsViewModel categoryOptions = new CategoryOptionsViewModel(
                            WebClient,
                            Dictionaries,
                            IsEditingAllowed,
                            IsLockedByCurrentEmployee);

                        await categoryOptions.LoadOptionsDataAsync(SelectedCategory.Id, CategoryOptionsInitializer);

                        CategoryOptions = categoryOptions;
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to load categories");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task SetCategoryAsync(CategoryViewItem arg)
        {
            IsLongOperationInProgress = true;

            try
            {
                CategoryOptions = EmptyCategoryOptions;

                if (arg == null)
                {
                    return;
                }

                CategoryOptionsViewModel categoryOptions = new CategoryOptionsViewModel(
                    WebClient,
                    Dictionaries,
                    IsEditingAllowed,
                    IsLockedByCurrentEmployee);

                await categoryOptions.LoadOptionsDataAsync(arg.Id, CategoryOptionsInitializer);

                CategoryOptions = categoryOptions;
            }
            catch (Exception exception)
            {
                SelectedCategory = null;
                Logger.LogError(exception, "Failed to load category options");
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private bool CanShowCreateCategoryDialog(CategoryViewItem selectedCategory)
        {
            return selectedCategory != null && (IsAdmin || IsMarketer) && !IsLockedByCurrentEmployee;
        }

        private void ShowCreateCategoryDialog(CategoryViewItem selectedCategory)
        {
            CreateCategoryDocumentManagerService.ShowView<CreateCategoryViewModel>(
                new object[] { selectedCategory.Id, GetFullName(selectedCategory.Id, Categories) },
                this);
        }

        private void ShowSaveOptionsDialog()
        {
            if (!CanSave())
            {
                MessageFacadeService.ShowNotificationWarning("Нечего сохранять");
                return;
            }

            CategoryOptionsSaveViewModel viewModel = new CategoryOptionsSaveViewModel(WebClient, Dictionaries, MessageFacadeService)
            {
                CategoryOptionsViewModel = CategoryOptions,
                CategoryFullName = GetFullName(SelectedCategory.Id, Categories, " → ")
            };
            viewModel.SetParentViewModel(this);

            IDocument document = SaveCategoryDocumentManagerService.CreateDocument("CategoryOptionsSaveView", viewModel);
            document.Id = Guid.NewGuid().ToString();
            document.Show();

            if (viewModel.IsOk)
            {
                CategoryViewItem category = Categories.FirstOrDefault(x => x.Id == viewModel.CategoryOptionsViewModel.Id);

                if (category != null)
                {
                    category.Name = viewModel.CategoryOptionsViewModel.Name;
                    category.Active = viewModel.CategoryOptionsViewModel.Active;
                    category.UseInTradeIn = viewModel.CategoryOptionsViewModel.UseInTradeIn;
                }

                SelectedCategory = category;

                CategoryOptions = viewModel.CategoryOptionsViewModel;
                CategoryOptionsInitializer.InitializeCategoryOptions(CategoryOptions);

                CancelCommand.Execute(null);
            }
        }

        private bool CanSave()
        {
            IReadOnlyCollection<CategoryOptionSaveDto> changedOptions = CategoryOptions?.GetChangedOptionsSaveDtos();

            if (changedOptions == null)
            {
                return false;
            }

            bool hasChangedOptions = changedOptions.Count > 0;
            return (hasChangedOptions || !string.Equals(CategoryOptions.Target?.Name, CategoryOptions.Category?.Name, StringComparison.Ordinal)) && (SelectedCategory?.Id ?? -1) == CategoryOptions.Id;
        }

        private async Task LockAsync()
        {
            EmployeeContextDto currentEmployee = WebClient.AuthenticatedEmployee;

            if (!currentEmployee.HasAnyRole(Role.Admin, Role.Marketer)
                && !WebClient.IsOperationAllowed(BusinessOperation.CategorySetTradeInCosts)
                && !WebClient.IsOperationAllowed(BusinessOperation.CreditOfferMinCategoryMarginPercentUpdate)
                && currentEmployee.HasAnyRole(Role.Product)
                && SelectedCategory.EmployeeId != currentEmployee.Id)
            {
                MessageFacadeService.ShowNotificationWarning($"Вы не ответственный в категории {SelectedCategory.Name}");
                return;
            }

            IsLongOperationInProgress = true;

            try
            {
                LockResponse<List<CategoryLockInfo>> lockResponse = await WebClient.ExecuteApiRequestAsync(new TryLockCategory(SelectedCategory.Id));

                if (lockResponse.Success)
                {
                    IsLockedByCurrentEmployee = true;
                    CategoryOptions.IsOptionsLockedByCurrentEmployee = true;
                }
                else
                {
                    CategoryFullDto categoryFromServer = await WebClient.ExecuteApiRequestAsync(new QueryCategoryFull(SelectedCategory.Id));
                    SelectedCategory = Mapper.Map(categoryFromServer, SelectedCategory);

                    if (categoryFromServer.EmployeeLock == null)
                    {
                        string lockedCategories = string.Join(Environment.NewLine, lockResponse.Dto);
                        MessageFacadeService.ShowNotificationWarning($"В дереве уже есть забл. категории: {Environment.NewLine}{lockedCategories}");
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationWarning($"Категория уже заблокирована пользователем {categoryFromServer.EmployeeLock?.ShortName}");
                    }
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(string.Join(" ", exception.GetErrorItems().Select(x => x.Message)));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to initialize category edit");
                MessageFacadeService.ShowNotificationError("Ошибка при редактировании категории");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private async Task CancelCommandAsync()
        {
            IsLongOperationInProgress = true;

            try
            {
                LockResponse<List<CategoryLockInfo>> lockResponse = await WebClient.ExecuteApiRequestAsync(new TryUnlockCategory(SelectedCategory.Id));

                if (lockResponse.Success)
                {
                    IsLockedByCurrentEmployee = false;
                    CategoryOptions.IsOptionsLockedByCurrentEmployee = false;
                    CategoryOptions.ResetAllChanges();
                }
                else
                {
                    CategoryFullDto categoryFromServer = await WebClient.ExecuteApiRequestAsync(new QueryCategoryFull(SelectedCategory.Id));
                    SelectedCategory = Mapper.Map(categoryFromServer, SelectedCategory);
                    MessageFacadeService.ShowNotificationWarning("Не удалось разблокировать категорию");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to unlock category");
                MessageFacadeService.ShowNotificationError("Ошибка при разблокировании категории");
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }

        private bool CanReorder(CategoryViewItem selectedCategory)
        {
            return !IsLockedByCurrentEmployee;
        }

        private void Reorder(CategoryViewItem selectedCategory)
        {
            int categoryId = selectedCategory?.Id ?? 1;

            CategoryViewItem[] childrenCategories = Categories.Where(x => x.ParentId == categoryId).ToArray();

            if (!childrenCategories.Any())
            {
                MessageFacadeService.ShowNotificationWarning("У данной категории нет дочерних категорий");
                return;
            }

            IEnumerable<ComboBoxItem> children = childrenCategories
                .OrderBy(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name));

            ReorderItemsViewModel viewModel = DialogDocumentManagerService.ShowView<ReorderItemsViewModel>(
            new ReorderItemsParameter("Изменить порядок дочерних категорий", children, okCommand: x => ReorderHandleOkAsync(x, categoryId)),
            this);

            if (!viewModel.IsOk)
            {
                return;
            }

            Dictionary<int, int> positions = viewModel.Items.Select((x, i) => new { x.Id, i }).ToDictionary(x => x.Id, x => x.i);

            foreach (CategoryViewItem x in childrenCategories)
            {
                x.Position = positions[x.Id];
            }
        }

        private async Task<bool> ReorderHandleOkAsync(IEnumerable<ComboBoxItem> items, int categoryId)
        {
            IReadOnlyCollection<CategoryPositionDto> categoryPositions = items.Select((x, i) => new CategoryPositionDto { Id = x.Id, Position = i }).ToArray();

            CategoryReorderChildrenDto dto = new CategoryReorderChildrenDto
            {
                Id = categoryId,
                ChildrenPositions = categoryPositions
            };

            try
            {
                Result<object> result = await WebClient.ExecuteApiRequestAsync(new ReorderCategoryChildren(dto));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Изменения сохранены с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Изменения успешно сохранены");
                }

                return true;
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to order children categories");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении изменений");
                return false;
            }
        }

        private bool CanMove(CategoryViewItem selectedCategory)
        {
            return selectedCategory != null && selectedCategory.ParentLevel <= 0 && !IsLockedByCurrentEmployee;
        }

        private void Move(CategoryViewItem selectedCategory)
        {
            CategoryMoveViewModel viewModel = DialogDocumentManagerService.ShowView<CategoryMoveViewModel>(
                selectedCategory.Id,
                this);

            if (viewModel.IsOk)
            {
                RefreshCommand.Execute(null);
            }
        }

        private void OnCategoryMessage(CategoryMessage message)
        {
            if (message.MessageType == MessageType.Added)
            {
                CategoryViewItem newCategory = Mapper.Map<CategoryViewItem>(message.Entity);
                Categories.Add(newCategory);
                SelectedCategory = newCategory;
            }
        }

        private bool ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItems),
                this);

            return viewModel.IsOk;
        }

        private void Robot()
        {
            string name = GetFullName(SelectedCategory.Id, Categories, " → ");

            RobotCategoryParameter parameter = new RobotCategoryParameter(SelectedCategory.Id, name);

            NonModalSizeableDialogDocumentManagerService.ShowView<RobotCategoryPropertyViewModel>(parameter, this);
        }
    }
}