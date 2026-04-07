using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Order.Actions;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Helpers;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Store.Order.OrderPaymentInfo;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order.OrderEditPrice
{
    public sealed class OrderEditPriceViewModel : TelemartDialogViewModelBase
    {
        private int orderId;

        private IReadOnlyDictionary<int, int> contractorEmployees;
        private IReadOnlyDictionary<int, string> employeeNames;

        public OrderEditPriceViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mapper = mapper;

            OrderPaymentViewModel = new OrderPaymentInfoViewModel();

            HandleCellValueChangedCommand = new DelegateCommand(() => OrderPaymentViewModel.CalcPaymentInfo(Order));
        }

        public OrderEditPriceViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandleCellValueChangedCommand { get; }

        #endregion

        public OrderEditPriceViewItem Order
        {
            get { return GetProperty(() => Order); }
            set { SetProperty(() => Order, value); }
        }

        public bool FreeDelivery
        {
            get { return GetProperty(() => FreeDelivery); }
            set { SetProperty(() => FreeDelivery, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public OrderPaymentInfoViewModel OrderPaymentViewModel
        {
            get { return GetProperty(() => OrderPaymentViewModel); }
            private set { SetProperty(() => OrderPaymentViewModel, value); }
        }

        private IMessenger Messenger { get; }

        private IMapper Mapper { get; }

        public static void BuildMetadata(MetadataBuilder<OrderEditPriceViewModel> builder)
        {
            builder.Property(x => x.Comment).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            orderId = (int)Parameter;

            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(orderId));

            FreeDelivery = order.Options?.FreeDelivery == true;

            Order = Mapper.Map<OrderEditPriceViewItem>(order);

            Order.OrderProducts.ForEach(x => x.NewPrice = x.PriceOut);

            await Task.WhenAll(RefreshContractorsAsync(), RefreshEmployeeNamesAsync());

            SummaryItems = GetSummaryItems();
            OrderPaymentViewModel.CalcPaymentInfo(Order);

            Title = $"Изменение стоимости товаров в заказе №{orderId}";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this) || Order.OrderProducts.Any(x => IDataErrorInfoHelper.HasErrors(x)))
            {
                return;
            }

            if (!MessageFacadeService.Confirm("Вы подтверждаете изменение стоимости товаров?"))
            {
                return;
            }

            try
            {
                UpdateOrderPrice.OrderProductPriceSaveDto[] productPrices = Order.OrderProducts.Where(x => x.ProductTypeId != ProductType.GuestProductId)
                    .Select(x => new UpdateOrderPrice.OrderProductPriceSaveDto(x.Id, x.ProductId, x.NewPrice!.Value))
                    .ToArray();

                UpdateOrderPrice gatewayRequest = new UpdateOrderPrice(orderId, FreeDelivery, Comment, productPrices);

                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                Messenger.Send(new OrderMessage(result.Data, MessageType.Changed));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Стоимость товаров в заказе №{orderId} изменена с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Стоимость товаров в заказе №{orderId} изменена успешно");
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении стоимости товаров");
                ShowValidationResultView("Ошибки при изменении стоимости товаров", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to edit products prices");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при изменении стоимости товаров");
                Logger.LogError(exception, "Error while editing products prices");
            }
        }

        private async Task RefreshContractorsAsync()
        {
            List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();

            contractorEmployees = contractors.ToDictionary(x => x.Id, y => y.EmployeeId);
        }

        private async Task RefreshEmployeeNamesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            employeeNames = employees.ToDictionary(x => x.Id, y => y.Name);
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems()
        {
            const string EmptyValue = "---";

            string manager = EmptyValue;

            if (Order.ClientId.HasValue && contractorEmployees.TryGetValue(Order.ClientId.Value, out int employeeId))
            {
                manager = employeeNames.GetValueOrDefault(employeeId, EmptyValue);
            }

            yield return new SummaryViewItem("Создал", employeeNames.GetValueOrDefault(Order.CreatedBy));
            yield return new SummaryViewItem("Менеджер", manager);
            yield return new SummaryViewItem("Статус", Dictionaries.GetItemById<OrderStatus>(Order.StateId)?.Name);
            yield return new SummaryViewItem("Заказ", Order.Id.ToString());

            yield return OrderHelper.GetPayedInfoSummaryItem(Order.Pko, Order.PaymentId, "Оплата");
        }
    }
}