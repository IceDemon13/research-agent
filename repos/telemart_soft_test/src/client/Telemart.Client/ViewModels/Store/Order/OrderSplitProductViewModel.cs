using System;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderSplitProductViewModel : TelemartDialogViewModelBase
    {
        private int orderId;
        private int orderProductId;
        private OrderFolderDto orderFolder;
        private bool splitAssemblyGroup;

        public OrderSplitProductViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public OrderSplitProductViewModel()
        {
        }

        public int MaxSplitQuantity
        {
            get { return GetProperty(() => MaxSplitQuantity); }
            private set { SetProperty(() => MaxSplitQuantity, value); }
        }

        public int SplitQuantity
        {
            get { return GetProperty(() => SplitQuantity); }
            set { SetProperty(() => SplitQuantity, value); }
        }

        public Result<OrderDto> Result { get; private set; }

        protected override Task HandleLoadedAsync()
        {
            OrderSplitProductParameter parameter = (OrderSplitProductParameter)Parameter;

            orderId = parameter.OrderId;
            orderProductId = parameter.OrderProductId;
            orderFolder = parameter.OrderFolder;
            splitAssemblyGroup = parameter.IsAssemblyVirtualProduct;

            MaxSplitQuantity = parameter.Quantity - 1;
            SplitQuantity = 1;

            Title = "Разделение товара";

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
                IRestClientGatewayRequest<Result<OrderDto>> request = splitAssemblyGroup
                    ? (IRestClientGatewayRequest<Result<OrderDto>>)new SplitOrderFolder(orderId, orderFolder.Id, SplitQuantity)
                    : (IRestClientGatewayRequest<Result<OrderDto>>)new OrderSplitProduct(orderId, orderProductId, SplitQuantity);

                Result = await WebClient.ExecuteApiRequestAsync(request);

                IsOk = true;
                Close();

                MessageFacadeService.ShowNotificationInfo("Товар успешно разделен");
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при разделении товара", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при разделении товара", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while splitting order product");
                MessageFacadeService.ShowNotificationError("Ошибка при разделении товара");
            }
        }
    }
}