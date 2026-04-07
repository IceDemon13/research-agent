using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Cities
{
    public class CityCreateViewModel : TelemartDialogViewModelBase
    {
        public CityCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;

            Title = "Создание города";
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

        public static void BuildMetadata(MetadataBuilder<CityCreateViewModel> builder)
        {
            builder.Property(x => x.Name).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameUkr).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.NameEn).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                Result<CityDto> result = await WebClient.ExecuteApiRequestAsync(new CreateCity(Name, NameUkr, NameEn));

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "Город создан с ошибками";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Город успешно создан");
                }

                Messenger.Send(new CityMessage(result.Data, MessageType.Added));

                Messenger.Send(new CityViewMessage(result.Data.Id));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании города");
                ShowValidationResultView("Ошибки при создании города", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create city");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании города");
                Logger.LogError(exception, "Error while creating city");
            }
        }
    }
}