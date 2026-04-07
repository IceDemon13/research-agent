using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Category;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class FeatureGroupCreateViewModel : TelemartDialogViewModelBase
    {
        public FeatureGroupCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public FeatureGroupCreateViewModel()
        {
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Categories
        {
            get { return GetProperty(() => Categories); }
            private set { SetProperty(() => Categories, value); }
        }

        public int CategoryId
        {
            get { return GetProperty(() => CategoryId); }
            private set { SetProperty(() => CategoryId, value); }
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

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<FeatureGroupCreateViewModel> builder)
        {
            const int MaxLength = 40;

            builder.Property(x => x.Name)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(MaxLength, () => $"Длина поля должна быть не длиннее {MaxLength} символов");
            builder.Property(x => x.NameUkr)
               .Required(() => Resources.RequiredErrorMessage)
               .MaxLength(MaxLength, () => $"Длина поля должна быть не длиннее {MaxLength} символов");
            builder.Property(x => x.NameEn)
                .Required(() => Resources.RequiredErrorMessage)
                .MaxLength(MaxLength, () => $"Длина поля должна быть не длиннее {MaxLength} символов");
        }

        protected override async Task HandleLoadedAsync()
        {
            CategoryId = (int)Parameter;

            List<CategoryDto> categories = await WebClient.ExecuteApiRequestAsync(new QueryCategories(), true).GetPagedResultDataAsync();

            Categories = categories.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();

            Title = "Создание группы характеристик";
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                FeatureGroupCreateDto dto = new FeatureGroupCreateDto
                {
                    CategoryId = CategoryId,
                    Name = Name,
                    NameUkr = NameUkr,
                    NameEn = NameEn
                };

                Result<FeatureGroupSimpleDto> result = await WebClient.ExecuteApiRequestAsync(new CreateFeatureGroup(dto));

                Messenger.Send(new FeatureGroupMessage(result.Data, MessageType.Added));

                IsOk = true;
                Close();

                MessageFacadeService.ShowNotificationInfo("Группа характеристик успешно создана");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании группы характеристик");
                ShowValidationResultView("Ошибки при создании группы характеристик", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create feature group");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании группы характеристик");
                Logger.LogError(exception, "Error while creating feature group");
            }
        }
    }
}