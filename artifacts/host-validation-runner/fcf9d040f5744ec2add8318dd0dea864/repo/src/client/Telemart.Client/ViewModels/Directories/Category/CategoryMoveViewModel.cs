using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Category
{
    internal sealed class CategoryMoveViewModel : TelemartDialogViewModelBase
    {
        public CategoryMoveViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
        }

        public CategoryMoveViewModel()
        {
        }

        public event Action OnDataLoaded;

        #region Properties

        public ObservableCollection<CategoryViewItem> Categories
        {
            get { return GetProperty(() => Categories); }
            set { SetProperty(() => Categories, value); }
        }

        public CategoryViewItem TargetCategory
        {
            get { return GetProperty(() => TargetCategory); }
            set { SetProperty(() => TargetCategory, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            set { SetProperty(() => CategoryId, value); }
        }

        #endregion

        private IMapper Mapper { get; }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories());

            CategoryViewItem root = CreateRootCategory();

            ObservableCollection<CategoryViewItem> categoryViewItems = categories.Data
                .Select(x => Mapper.Map<CategoryViewItem>(x))
                .ToObservableCollection();

            categoryViewItems.Add(root);

            Categories = categoryViewItems;
            CategoryId = (int)Parameter;

            Title = "Перенос категории";

            OnDataLoaded?.Invoke();
        }

        protected override async Task HandleOkAsync()
        {
            if (TargetCategory == null)
            {
                MessageFacadeService.ShowNotificationWarning("Целевая категория не выбрана");
                return;
            }

            LockResponse<List<CategoryLockInfo>> response = await LockAsync(CategoryId);

            if (response?.Success == true)
            {
                try
                {
                    Result<object> result = await WebClient.ExecuteApiRequestAsync(new MoveCategory(CategoryId, TargetCategory.Id));

                    if (result.Warnings.Any())
                    {
                        MessageFacadeService.ShowNotificationWarning("Категория перенесена с предупреждениями");
                        ShowValidationResultView("Предупреждения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                    }
                    else
                    {
                        MessageFacadeService.ShowNotificationInfo("Категория перенесена успешно");
                    }

                    IsOk = true;
                    Close();
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                    ShowValidationResultView("Ошибки при выполнении операции", exception.GetErrorItems());
                }
                catch (UnexpectedErrorException exception)
                {
                    Logger.LogError(exception, "Failed to save entity");
                    ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                }
                catch (Exception exception)
                {
                    Logger.LogError(exception, "Failed to move category");
                    MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                }

                await UnlockAsync(CategoryId);
            }
        }

        private static CategoryViewItem CreateRootCategory()
        {
            CategoryViewItem root = CategoryViewItem.Create();

            root.Id = 1;
            root.ParentId = 0;
            root.Active = 1;
            root.EmployeeId = 1;
            root.Name = "Главная";
            root.NameFull = "Главная";
            root.NameFullUkr = "Головна";
            root.Manufactor = string.Empty;
            root.ParentLevel = -1;
            root.Level = 0;
            root.IsParent = false;
            root.Left = 1;
            root.Right = 50000;

            return root;
        }

        private async Task<LockResponse<List<CategoryLockInfo>>> LockAsync(int entityId)
        {
            LockResponse<List<CategoryLockInfo>> response = null;

            try
            {
                response = await WebClient.ExecuteApiRequestAsync(new TryLockCategory(entityId));

                if (!response.Success)
                {
                    string lockedCategories = string.Join(Environment.NewLine, response.Dto);
                    MessageFacadeService.ShowNotificationWarning($"В дереве уже есть забл. категории: {Environment.NewLine}{lockedCategories}");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to lock category");
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
            }

            return response;
        }

        private async Task<LockResponse<List<CategoryLockInfo>>> UnlockAsync(int entityId)
        {
            LockResponse<List<CategoryLockInfo>> response = null;

            try
            {
                response = await WebClient.ExecuteApiRequestAsync(new TryUnlockCategory(entityId));

                if (!response.Success)
                {
                    MessageFacadeService.ShowNotificationError("Не удалось разблокировать категорию");
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to unlock category");
                MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
            }

            return response;
        }
    }
}