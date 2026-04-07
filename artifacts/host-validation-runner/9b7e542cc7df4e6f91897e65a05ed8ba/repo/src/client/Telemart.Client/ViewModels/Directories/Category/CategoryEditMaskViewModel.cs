using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Directories.Category
{
    public sealed class CategoryEditMaskViewModel : TelemartDialogViewModelBase
    {
        private int categoryId;
        private int languageId;

        public CategoryEditMaskViewModel(IWebClient webClient, IDictionaries dictionaries, IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public CategoryEditMaskViewModel()
        {
        }

        public string Mask
        {
            get { return GetProperty(() => Mask); }
            set { SetProperty(() => Mask, value); }
        }

        #region DialogSettings

        public override int Height => 293;

        public override int MinHeight => 225;

        public override int MinWidth => 400;

        public override int Width => 520;

        public override int MaxHeight => 484;

        public override int MaxWidth => 640;

        #endregion

        protected override async Task HandleLoadedAsync()
        {
            CategoryEditMaskParameter parameter = (CategoryEditMaskParameter)Parameter;
            categoryId = parameter.CategoryId;
            languageId = parameter.LanguageId;

            CategoryFullDto category = await WebClient.ExecuteApiRequestAsync(new QueryCategoryFull(categoryId));

            switch (languageId)
            {
                case Language.RussianId:
                    Mask = category.FmaskT;
                    break;
                case Language.UkrainianId:
                    Mask = category.FmaskTUa;
                    break;
                case Language.EnglishId:
                    Mask = category.FmaskTEn;
                    break;
            }

            Title = Resources.CategoryEditingData_Mask;
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                UpdateCategoryMask request = new UpdateCategoryMask(categoryId, Mask, languageId);

                Result<CategoryFullDto> result = await WebClient.ExecuteApiRequestAsync(request);

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Маска сохранена с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Маска успешно сохранена");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
                ShowValidationResultView("Ошибки при сохранении", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to save features mask");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while saving features mask");
                MessageFacadeService.ShowNotificationError("Ошибка при сохранении");
            }
        }
    }
}