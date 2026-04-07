using System;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.FeatureGroup;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.Content;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Content.ProductFeatureGroups
{
    public sealed class FeatureGroupViewModel : TelemartDialogViewModelBase
    {
        private int groupId;

        public FeatureGroupViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public FeatureGroupViewModel()
        {
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

        public static void BuildMetadata(MetadataBuilder<FeatureGroupViewModel> builder)
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

        protected override Task HandleLoadedAsync()
        {
            FeatureGroupParameter parameter = (FeatureGroupParameter)Parameter;

            Name = parameter.Name;
            NameUkr = parameter.NameUkr;
            NameEn = parameter.NameEn;
            groupId = parameter.Id;

            Title = $"Группа характеристик №{groupId}";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                FeatureGroupSaveDto dto = new FeatureGroupSaveDto
                {
                    Id = groupId,
                    Name = Name,
                    NameUkr = NameUkr,
                    NameEn = NameEn
                };

                Result<FeatureGroupSimpleDto> result = await WebClient.ExecuteApiRequestAsync(new UpdateFeatureGroup(groupId, dto));

                Messenger.Send(new FeatureGroupMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();

                MessageFacadeService.ShowNotificationInfo("Группа характеристик успешно изменена");
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении группы характеристик");
                ShowValidationResultView("Ошибки при изменении группы характеристик", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to update feature group");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerConnectError, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении группы характеристик");
                Logger.LogError(exception, "Error while updating feature group");
            }
        }
    }
}