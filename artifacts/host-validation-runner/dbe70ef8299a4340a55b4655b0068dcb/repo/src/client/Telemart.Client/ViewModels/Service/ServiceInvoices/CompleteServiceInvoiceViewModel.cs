using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.ServiceInvoice.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceInvoices
{
    public sealed class CompleteServiceInvoiceViewModel : TelemartDialogViewModelBase
    {
        public CompleteServiceInvoiceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        #region INPC

        public IReadOnlyCollection<ServiceInvoiceProductViewItem> ServiceInvoiceProducts
        {
            get { return GetProperty(() => ServiceInvoiceProducts); }
            set { SetProperty(() => ServiceInvoiceProducts, value); }
        }

        public ServiceInvoiceProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value); }
        }

        #endregion

        private int ServiceInvoiceId { get; set; }

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        protected override Task HandleLoadedAsync()
        {
            CompleteServiceInvoiceParameter parameter = (CompleteServiceInvoiceParameter)Parameter;

            ServiceInvoiceId = parameter.ServiceInvoiceId;
            ServiceInvoiceProducts = parameter.ServiceInvoiceProducts;

            Title = $"Серв. накладная №{ServiceInvoiceId}";

            return Task.CompletedTask;
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            try
            {
                int acceptedProductsCount = ServiceInvoiceProducts.Count(x => x.Accepted);
                string confirmMessage = $"Вы подтверждаете, что СЦ принял {acceptedProductsCount} из {ServiceInvoiceProducts.Count} товаров?";

                if (acceptedProductsCount < ServiceInvoiceProducts.Count && !MessageFacadeService.Confirm(confirmMessage))
                {
                    return;
                }

                List<RepairInvoiceDto> repairInvoices = Mapper.Map<List<RepairInvoiceDto>>(ServiceInvoiceProducts);
                CompleteServiceInvoiceDto completeServiceInvoiceDto = new CompleteServiceInvoiceDto { RepairInvoices = repairInvoices };
                Result<ServiceInvoiceDto> result = await WebClient.ExecuteApiRequestAsync(new CompleteServiceInvoice(ServiceInvoiceId, completeServiceInvoiceDto));

                MessageFacadeService.ShowNotificationInfo("Серв. накладная успешно завершена");
                Messenger.Send(new ServiceInvoiceMessage(result.Data, MessageType.Changed));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при завершении серв. накладной");
                ShowValidationResultView("Ошибки при завершении серв. накладной", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to complete service invoice");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to complete service invoice. ServiceInvoiceId: {ServiceInvoiceId}", ServiceInvoiceId);
                MessageFacadeService.ShowNotificationError("Ошибка при завершении серв. накладной");
            }
        }
    }
}