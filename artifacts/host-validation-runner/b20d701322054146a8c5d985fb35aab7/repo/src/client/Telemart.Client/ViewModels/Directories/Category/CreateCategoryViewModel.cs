using System;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Validation;
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
    public sealed class CreateCategoryViewModel : TelemartDialogViewModelBase
    {
        public CreateCategoryViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        public CreateCategoryViewModel()
        {
        }

        #region INPC

        public int ParentId
        {
            get { return GetProperty(() => ParentId); }
            set { SetProperty(() => ParentId, value); }
        }

        public string CategoryParentFullName
        {
            get { return GetProperty(() => CategoryParentFullName); }
            set { SetProperty(() => CategoryParentFullName, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public string NameUkr
        {
            get { return GetProperty(() => NameUkr); }
            set { SetProperty(() => NameUkr, value); }
        }

        public string NameEn
        {
            get { return GetProperty(() => NameEn); }
            set { SetProperty(() => NameEn, value); }
        }

        public CategoryCreateType Type
        {
            get { return GetProperty(() => Type); }
            set { SetProperty(() => Type, value); }
        }

        public string LinkRewrite
        {
            get { return GetProperty(() => LinkRewrite); }
            set { SetProperty(() => LinkRewrite, value); }
        }

        #endregion

        public bool IsCurrentUserAdmin => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Admin);

        public bool IsCurrentUserMarketer => WebClient.AuthenticatedEmployee.HasAnyRole(Role.Marketer);

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<CreateCategoryViewModel> builder)
        {
            builder.Property(x => x.Name).ApplyCategoryNameValidationRules();
            builder.Property(x => x.NameUkr).ApplyCategoryNameUkrValidationRules();

            builder.Property(x => x.NameEn).Required(() => "Имя категории должно быть заполнено");

            builder.Property(x => x.LinkRewrite)
                .Required(() => "Ссылка должна быть заполнена")
                .MinLength(3, () => "Минимальная длина 3 символа")
                .MatchesRegularExpression(@"^[a-z0-9]{1}[a-z0-9-\.]*[a-z0-9]+$", () => "В ссылке могут быть только строчные латинские буквы, цифры и дефис");
        }

        protected override Task HandleLoadedAsync()
        {
            object[] p = (object[])Parameter;

            ParentId = (int)p[0];
            CategoryParentFullName = (string)p[1];

            Title = "Создание категории";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (Type == CategoryCreateType.None)
            {
                MessageFacadeService.ShowNotificationWarning("Выберите тип категории");
                return;
            }

            try
            {
                Result<CategoryDto> result = await WebClient.ExecuteApiRequestAsync(new CreateCategory(ParentId, Name, NameUkr, NameEn, LinkRewrite, Type));

                MessageFacadeService.ShowNotificationInfo("Категория создана успешно");

                Messenger.Send(new CategoryMessage(result.Data, MessageType.Added));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании категории");
                ShowValidationResultView("Ошибки", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании категории");
                ShowValidationResultView("Ошибки", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
                Logger.LogError(exception, "Failed to create category");
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании категории");
                Logger.LogError(exception, "Failed to create category");
            }
        }
    }
}