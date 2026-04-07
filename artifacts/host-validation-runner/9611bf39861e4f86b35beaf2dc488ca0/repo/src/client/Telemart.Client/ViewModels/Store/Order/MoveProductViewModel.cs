using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.City;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.City;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class MoveProductViewModel : TelemartDialogViewModelBase
    {
        private OrderDto sourceOrder;

        public MoveProductViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Mapper = mapper;
            Messenger = messenger;

            RowDoubleClickCommand = new DelegateCommand<RowDoubleClickEventArgs>(RowDoubleClick);
            HandlePreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(HandlePreviewKeyDown);
            CreateOrderCommand = new AsyncCommand(CreateOrderAsync);
        }

        public MoveProductViewModel()
        {
        }

        #region Commands

        public IDelegateCommand HandlePreviewKeyDownCommand { get; }

        public IDelegateCommand RowDoubleClickCommand { get; }

        public IAsyncCommand CreateOrderCommand { get; }

        #endregion

        #region INPC

        public ObservableRangeCollection<MoveProductViewModel.TargetOrderViewItem> Orders
        {
            get { return GetProperty(() => Orders); }
            private set { SetProperty(() => Orders, value); }
        }

        public TargetOrderViewItem SelectedOrder
        {
            get { return GetProperty(() => SelectedOrder); }
            set { SetProperty(() => SelectedOrder, value); }
        }

        public ObservableCollection<ValidationResultItem> ValidationItems
        {
            get { return GetProperty(() => ValidationItems); }
            private set { SetProperty(() => ValidationItems, value); }
        }

        public ObservableCollection<OrderProductViewItem> OrderProducts
        {
            get { return GetProperty(() => OrderProducts); }
            private set { SetProperty(() => OrderProducts, value); }
        }

        public ObservableCollection<OrderProductViewItem> SelectedOrderProducts { get; } = new ObservableCollection<OrderProductViewItem>();

        public bool OpenOrders
        {
            get { return GetProperty(() => OpenOrders); }
            set { SetProperty(() => OpenOrders, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 600;

        public override int MinHeight => 480;

        public override int MinWidth => 640;

        public override int Width => 800;

        #endregion

        private IMapper Mapper { get; }

        private IMessenger Messenger { get; }

        protected override bool CanOk()
        {
            return ValidationItems == null;
        }

        protected override async Task HandleLoadedAsync()
        {
            OpenOrders = true;

            sourceOrder = (OrderDto)Parameter;

            OrderViewItem orderViewItem = Mapper.Map<OrderViewItem>(sourceOrder);

            OrderProducts = orderViewItem.Products;

            List<CityDto> cities = await WebClient.ExecuteApiRequestAsync(new QueryCities(), true).GetPagedResultDataAsync();

            Subdivision subdivision = Dictionaries.GetItemById<Subdivision>(sourceOrder.SubdivisionId);
            int? cityId = cities.FirstOrDefault(x => x.Id == sourceOrder.CityId)?.Id;

            ObservableCollection<ValidationResultItem> items = ValidateOrder(sourceOrder, subdivision, cityId).ToObservableCollection();

            if (!items.Any())
            {
                IFilteringItem filter = GetFilteringItem(sourceOrder, subdivision, cityId);

                PagedResult<OrderDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryOrders(filter));

                IOrderedEnumerable<TargetOrderViewItem> targetOrderViewItems = pagedResult.Data
                    .Where(x => x.Id != sourceOrder.Id)
                    .Select(x => new TargetOrderViewItem(x.Id, x.Fio, x.DeliveryTime))
                    .OrderByDescending(x => x.Id);

                Orders = new ObservableRangeCollection<TargetOrderViewItem> { CreateNewOrderViewItem() };
                Orders.AddRange(targetOrderViewItems);

                SelectedOrder = Orders.First();

                Title = "Выберите товары и заказ для переноса";
            }
            else
            {
                ValidationItems = items;
                Title = "Ошибки";
            }
        }

        protected override async Task HandleOkAsync()
        {
            if (SelectedOrder?.Id == null)
            {
                MessageFacadeService.ShowMessageBoxWarning("Нельзя переносить товары в не созданный заказ");
                return;
            }

            if (SelectedOrderProducts == null || SelectedOrderProducts.Count == 0)
            {
                MessageFacadeService.ShowMessageBoxWarning("Выберите товары для переноса");
                return;
            }

            int[] orderFolderIds = SelectedOrderProducts
                .Where(x => x.OrderFolderId.HasValue)
                .Select(x => x.OrderFolderId.Value)
                .ToArray();

            bool anyAssemblyFolders = sourceOrder.Folders
                .Any(x => orderFolderIds.Contains(x.Id)
                          && (x.TypeId == OrderFolderType.AssemblyServiceId ||
                              x.TypeId == OrderFolderType.AssembledComputerRuleId));

            if (anyAssemblyFolders)
            {
                MessageFacadeService.ShowMessageBoxWarning("Запрещено переносить товары, которые находятся в сборке или конфигурации");
                return;
            }

            HashSet<int> parentIds = SelectedOrderProducts.Where(x => x.ParentRecordId == null).Select(x => x.Id).ToHashSet();

            if (SelectedOrderProducts.Any(x => x.IsGift && !parentIds.Contains(x.ParentRecordId!.Value)))
            {
                MessageFacadeService.ShowMessageBoxWarning("Нельзя переносить подарок без основного товара");
                return;
            }

            if (SelectedOrderProducts.Any(x => x.IsAdditionalService && !parentIds.Contains(x.ParentRecordId!.Value)))
            {
                MessageFacadeService.ShowMessageBoxWarning("Нельзя переносить услугу без основного товара");
                return;
            }

            HashSet<int> additionalServiceOrderProductIds = SelectedOrderProducts.Where(x => x.IsAdditionalService).Select(x => x.Id).ToHashSet();

            if (SelectedOrderProducts.Any(x => x.IsAdditionalServiceConsumable && !additionalServiceOrderProductIds.Contains(x.ParentRecordId!.Value)))
            {
                MessageFacadeService.ShowMessageBoxWarning("Нельзя переносить товар для оказания услуги без услуги");
                return;
            }

            if (!MessageFacadeService.Confirm("Вы уверены, что хотите перенести товары?"))
            {
                return;
            }

            try
            {
                MoveOrderProducts gatewayRequest = new MoveOrderProducts(sourceOrder.Id, SelectedOrder.Id.Value, GetOrderProductsToMove().ToArray());

                Result<OrderDto[]> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                foreach (OrderDto orderDto in result.Data)
                {
                    Messenger.Send(new OrderMessage(orderDto, MessageType.Changed));
                }

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Товары перенесены с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Товары успешно перенесены");
                }

                if (OpenOrders)
                {
                    foreach (OrderDto orderDto in result.Data)
                    {
                        Messenger.Send(new OrderEditViewMessage(orderDto.Id));
                    }
                }

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при переносе товаров");
                ShowValidationResultView("Ошибки при переносе товаров", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Error while moving order products");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while moving order products");
                MessageFacadeService.ShowNotificationError("Ошибка при переносе товаров");
            }
        }

        private static IEnumerable<ValidationResultItem> ValidateOrder(OrderDto orderObj, Subdivision subdivision, int? cityId)
        {
            if (orderObj.StateId != OrderStatus.Received.Id)
            {
                yield return new ValidationResultItem(
                    $"Переносить товары можно только из заказа в статусе \"{OrderStatus.Received.Name}\"",
                    true);
            }

            if (orderObj.PromoCodes != null && orderObj.PromoCodes.Any())
            {
                yield return new ValidationResultItem("Нельзя переносить товары из заказа, в котором есть акции", true);
            }

            if (orderObj.Bonuses != null && orderObj.Bonuses.Any())
            {
                yield return new ValidationResultItem("Нельзя переносить товары из заказа, в котором есть бонусы", true);
            }

            if (subdivision.IsRetail)
            {
                if (cityId == null)
                {
                    yield return new ValidationResultItem("В заказе не заполнен город", true);
                }

                if (string.IsNullOrWhiteSpace(orderObj.Phone))
                {
                    yield return new ValidationResultItem("В заказе не заполнен телефон", true);
                }
            }
        }

        private static OrderFilteringItem GetFilteringItem(OrderDto orderObj, Subdivision subdivision, int? cityId)
        {
            OrderFilteringItem filter = new OrderFilteringItem(string.Empty, new List<int>())
            {
                OrderStatuses = new List<int> { OrderStatus.Received.Id },
                Contractors = new List<int> { orderObj.ClientId }
            };

            if (subdivision.IsRetail)
            {
                filter.Cities = new List<int> { cityId.Value };
                filter.Cellphone = orderObj.Phone;
            }

            return filter;
        }

        private static TargetOrderViewItem CreateNewOrderViewItem()
        {
            return new TargetOrderViewItem(null, "Новый заказ", null);
        }

        private IEnumerable<int> GetOrderProductsToMove()
        {
            for (int i = 0; i < OrderProducts.Count; i++)
            {
                if (SelectedOrderProducts.Any(x => x.Id == OrderProducts[i].Id))
                {
                    yield return OrderProducts[i].Id;

                    int nextOrderProductIndex = i + 1;

                    while (nextOrderProductIndex < OrderProducts.Count && OrderProducts[nextOrderProductIndex].ParentRecordId.HasValue)
                    {
                        yield return OrderProducts[nextOrderProductIndex].Id;
                        nextOrderProductIndex++;
                        i++;
                    }
                }
            }
        }

        private void HandlePreviewKeyDown(KeyEventArgs e)
        {
            ////if (e.Key == Key.Enter)
            ////{
            ////    OkCommand.Execute(null);
            ////}
            ////else if (e.Key == Key.Escape)
            ////{
            ////    CancelCommand.Execute(null);
            ////}
        }

        private void RowDoubleClick(RowDoubleClickEventArgs args)
        {
            if (args.ChangedButton == MouseButton.Left)
            {
                OkCommand.Execute(null);
            }
        }

        private async Task CreateOrderAsync()
        {
            try
            {
                OrderCreateDto createDto = new OrderCreateDto
                {
                    Id = 0,
                    OrderProducts = new List<OrderProductSaveDto>(),
                    PromoCodes = new List<OrderProductPromoCodeSaveDto>(),
                    ClientId = sourceOrder.ClientId,
                    LastName = sourceOrder.LastName,
                    FirstName = sourceOrder.FirstName,
                    MiddleName = sourceOrder.MiddleName,
                    Phone = sourceOrder.Phone,
                    Phone2 = sourceOrder.Phone2,
                    Email = sourceOrder.Email,
                    WorkPlaceId = WebClient.WorkPlaceId,
                    CityId = sourceOrder.CityId,
                    CarryId = sourceOrder.CarryId,
                    PaymentId = sourceOrder.PaymentId,
                    WarehouseId = sourceOrder.WarehouseId,
                    Address = sourceOrder.Address,
                    DeliveryData = sourceOrder.DeliveryData
                };

                Result<OrderDto> result = await WebClient.ExecuteApiRequestAsync(new CreateOrder(createDto));

                Orders.Insert(1, new TargetOrderViewItem(result.Data.Id, result.Data.Fio, result.Data.DeliveryTime));

                Messenger.Send(new OrderMessage(result.Data, MessageType.Added));

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Заказ №{result.Data.Id.ToString(CultureInfo.CurrentUICulture)} создан с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Заказ №{result.Data.Id.ToString(CultureInfo.CurrentUICulture)} успешно создан");
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании заказа");
                ShowValidationResultView("Ошибки при создании заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create order");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Failed to create order");
                MessageFacadeService.ShowNotificationError("Ошибка при создании заказа");
            }
        }

        public class TargetOrderViewItem : BindableBase
        {
            public TargetOrderViewItem(int? id, string fio, DateTime? deliveryTime)
            {
                Id = id;
                Fio = fio;
                DeliveryTime = deliveryTime;
            }

            public DateTime? DeliveryTime
            {
                get { return GetProperty(() => DeliveryTime); }
                set { SetProperty(() => DeliveryTime, value); }
            }

            public string Fio
            {
                get { return GetProperty(() => Fio); }
                set { SetProperty(() => Fio, value); }
            }

            public int? Id
            {
                get { return GetProperty(() => Id); }
                set { SetProperty(() => Id, value); }
            }
        }
    }
}