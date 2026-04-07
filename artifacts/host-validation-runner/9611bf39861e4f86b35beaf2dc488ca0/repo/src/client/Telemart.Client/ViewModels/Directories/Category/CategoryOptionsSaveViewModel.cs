using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Category
{
    public sealed class CategoryOptionsSaveViewModel : TelemartDialogViewModelBase
    {
        private const string GenericSaveErrorLogMessage = "Failed to save category settings";
        private const string GenericSaveErrorMessage = "Ошибка при сохранении настроек";

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryOptionsSaveViewModel" /> class.
        /// </summary>
        /// <param name="webClient">The web client.</param>
        /// <param name="dictionaries">The dictionaries.</param>
        /// <param name="messageFacadeService">The notification service.</param>
        public CategoryOptionsSaveViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            SaveCommand = new AsyncCommand(SaveAsync);
        }

        #region Commands

        public IAsyncCommand SaveCommand { get; set; }

        #endregion

        #region INPC

        public string CategoryFullName
        {
            get { return GetProperty(() => CategoryFullName); }
            set { SetProperty(() => CategoryFullName, value); }
        }

        public IReadOnlyCollection<ICategoryOption> CategoryOptions
        {
            get { return GetProperty(() => CategoryOptions); }
            set { SetProperty(() => CategoryOptions, value); }
        }

        public CategoryOptionsViewModel CategoryOptionsViewModel
        {
            get { return GetProperty(() => CategoryOptionsViewModel); }
            set { SetProperty(() => CategoryOptionsViewModel, value); }
        }

        public ReadOnlyObservableCollection<CategoryOverrideOption> CategoryOverrideOptions
        {
            get { return GetProperty(() => CategoryOverrideOptions); }
            set { SetProperty(() => CategoryOverrideOptions, value); }
        }

        public bool IsLongOperationInProgress
        {
            get { return GetProperty(() => IsLongOperationInProgress); }
            set { SetProperty(() => IsLongOperationInProgress, value); }
        }

        #endregion

        public override int MaxHeight => 800;

        protected override Task HandleLoadedAsync()
        {
            CategoryOptions = CategoryOptionsViewModel.GetChangedOptions();
            CategoryOverrideOptions = Dictionaries.GetItems<CategoryOverrideOption>().ToReadOnlyObservableCollection();

            Title = "Сохранение";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            await SaveAsync();

            if (IsOk)
            {
                Close();
            }
        }

        private async Task SaveAsync()
        {
            if (CategoryOptionsViewModel == null)
            {
                return;
            }

            IsLongOperationInProgress = true;

            try
            {
                IReadOnlyCollection<CategoryOptionSaveDto> changedOptionsSaveDtos = CategoryOptionsViewModel.GetChangedOptionsSaveDtos();

                int categoryId = CategoryOptionsViewModel.Category.Id;

                CategoryOptionsSaveDto options = new CategoryOptionsSaveDto
                {
                    Options = changedOptionsSaveDtos,
                    CategoryId = categoryId
                };

                Result<CategoryFullDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateCategoryOptions(CategoryOptionsViewModel.Id, options));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Настройки сохранены с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Настройки успешно сохранены");
                }

                CategoryOptionsViewModel.SaveDiff(changedOptionsSaveDtos, result.Data.NameFull, result.Data.NameFullUkr, result.Data.NameFullEn);

                IsOk = true;
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError($"Ошибка при сохранении категории {CategoryOptionsViewModel.Category.Name}");
                ShowValidationResultView("Ошибки при сохранении категории", exception.GetErrorItems());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, GenericSaveErrorLogMessage);
                MessageFacadeService.ShowNotificationError(GenericSaveErrorMessage);
            }
            finally
            {
                IsLongOperationInProgress = false;
            }
        }
    }
}