using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Barcode;
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
    public class ProductBarcodeCreateViewModel : TelemartDialogViewModelBase
    {
        private ProductBarcodeCreateParameter parameter;

        public ProductBarcodeCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public string Barcode
        {
            get { return GetProperty(() => Barcode); }
            set { SetProperty(() => Barcode, value); }
        }

        private IMessenger Messenger { get; }

        protected override Task HandleLoadedAsync()
        {
            parameter = (ProductBarcodeCreateParameter)Parameter;

            Title = "Введите штрих-код";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            try
            {
                Barcode = Regex.IsMatch(Barcode, @"^\d{12}$")
                    ? $"0{Barcode}"
                    : Barcode;

                Ean13Barcode ean13Barcode = new Ean13Barcode(Barcode);

                if (!ean13Barcode.IsValid)
                {
                    MessageFacadeService.ShowNotificationWarning("Неверный ШК");
                    return;
                }
            }
            catch
            {
                MessageFacadeService.ShowNotificationError("Ошибка при распознавании ШК");
                return;
            }

            try
            {
                ProductBarcodeSaveDto dto = new ProductBarcodeSaveDto
                {
                    Barcode = Barcode,
                    ProductId = parameter.ProductId
                };

                Result<ProductBarcodeDto> result = await WebClient.ExecuteApiRequestAsync(new CreateProductBarcode(parameter.ProductId, dto));

                Messenger.Send(new ProductBarcodeMessage(result.Data, MessageType.Added));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании штрих-кода");
                ShowValidationResultView("Ошибки при создании штрих-кода", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create product barcode");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании штрих-кода");
                Logger.LogError(exception, "Error while creating product barcode");
            }
        }
    }
}