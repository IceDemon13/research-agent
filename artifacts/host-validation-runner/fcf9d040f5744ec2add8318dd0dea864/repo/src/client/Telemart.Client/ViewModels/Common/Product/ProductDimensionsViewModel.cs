using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Common.Product
{
    public class ProductDimensionsViewModel : TelemartDialogViewModelBase
    {
        public ProductDimensionsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public ProductDimensionsViewItem Dimensions
        {
            get { return GetProperty(() => Dimensions); }
            private set { SetProperty(() => Dimensions, value); }
        }

        private IMessenger Messenger { get; }

        protected override Task HandleLoadedAsync()
        {
            Dimensions = (ProductDimensionsViewItem)Parameter;

            Title = "Внесите ВГХ";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(Dimensions))
            {
                return;
            }

            try
            {
                UpdateProductDimensions request = new UpdateProductDimensions(
                    Dimensions.Id,
                    Dimensions.Width.Value,
                    Dimensions.Height.Value,
                    Dimensions.Depth.Value,
                    Dimensions.Weight.Value);

                Result<ProductDimensionsDto> result = await WebClient.ExecuteApiRequestAsync(request);

                if (result.Warnings.Any())
                {
                    IReadOnlyCollection<ValidationResultItem> validationResultItems = result
                        .Warnings
                        .Select(x => new ValidationResultItem(x, false))
                        .ToList();

                    string message = "ВГХ обновлены с предупреждениями";

                    ShowValidationResultView(message, validationResultItems);

                    MessageFacadeService.ShowNotificationWarning(message);
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("ВГХ успешно обновлены");
                }

                Messenger.Send(new ProductDimensionsMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении ВГХ");
                ShowValidationResultView("Ошибки при обновлении ВГХ", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to update product dimensions");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при обновлении ВГХ");
                Logger.LogError(exception, "Error while updating product dimensions");
            }
        }
    }
}