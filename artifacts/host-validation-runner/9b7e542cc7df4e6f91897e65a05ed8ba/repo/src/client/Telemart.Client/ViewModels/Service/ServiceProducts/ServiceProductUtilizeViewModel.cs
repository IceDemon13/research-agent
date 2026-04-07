using System;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.ServiceProduct.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ServiceProduct;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public class ServiceProductUtilizeViewModel : TelemartDialogViewModelBase
    {
        private int serviceProductId;

        public ServiceProductUtilizeViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messanger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messanger;
        }

        public string Description
        {
            get { return GetProperty(() => Description); }
            set { SetProperty(() => Description, value); }
        }

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<ServiceProductUtilizeViewModel> builder)
        {
            builder.Property(x => x.Description)
                .MinLength(10, () => "Минимальная длина 10 символов")
                .MaxLength(100, () => "Максимальная длина 100 символов")
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override Task HandleLoadedAsync()
        {
            serviceProductId = (int)Parameter;

            Title = "Утилизация сервисного товара";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                Result<ServiceProductDto> result = await WebClient.ExecuteApiRequestAsync(new OnUtilizationServiceProduct(serviceProductId, Description));

                Messenger.Send(new ServiceProductMessage(result.Data, MessageType.Changed));

                if (result.Warnings.Any())
                {
                    ShowValidationResultView(
                        "Предупрежедения при утилизации сервисного товара",
                        result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Сервисный товар №{serviceProductId} успешно утилизирован");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при утилизации сервисного товара");
                ShowValidationResultView("Ошибки при утилизации сервисного товара", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to utilize service product");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при утилизации сервисного товара");
                Logger.LogError(exception, "Error while utilizing service product");
            }
        }
    }
}