using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.AssemblyService.Actions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.AdditionalServiceProduct;
using Telemart.Client.ViewModels.AssemblyService;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class OrderExpireViewModel : TelemartDialogViewModelBase
    {
        private OrderDto _order;

        public OrderExpireViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IMediator mediator,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            Mediator = mediator;
            ErrorHandler = errorHandler;
        }

        public OrderExpireViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public int? SelectedWarehouse
        {
            get { return GetProperty(() => SelectedWarehouse); }
            set { SetProperty(() => SelectedWarehouse, value); }
        }

        public string TrackNumber
        {
            get { return GetProperty(() => TrackNumber); }
            set { SetProperty(() => TrackNumber, value); }
        }

        public string Comment
        {
            get { return GetProperty(() => Comment); }
            set { SetProperty(() => Comment, value); }
        }

        public bool NeedTrackNumber
        {
            get { return GetProperty(() => NeedTrackNumber); }
            private set { SetProperty(() => NeedTrackNumber, value, () => { RaisePropertyChanged(nameof(TrackNumber)); }); }
        }

        public CarryType CarryType
        {
            get { return GetProperty(() => CarryType); }
            private set { SetProperty(() => CarryType, value, () => { RaisePropertyChanged(nameof(TrackNumber)); }); }
        }

        #endregion

        private IMessenger Messenger { get; }

        private IMediator Mediator { get; }

        private IErrorHandler ErrorHandler { get; }

        public static void BuildMetadata(MetadataBuilder<OrderExpireViewModel> builder)
        {
            builder.Property(x => x.SelectedWarehouse)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.TrackNumber)
                .MatchesInstanceRule(
                    (x, y) => string.IsNullOrWhiteSpace(y.CarryType.TtnRegex) || (x != null && Regex.IsMatch(x, y.CarryType.TtnRegex)),
                    () => "Введите корректно номер ТТН");
        }

        protected override async Task HandleLoadedAsync()
        {
            _order = (OrderDto)Parameter;

            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id) && x.Active == 1)
                .OrderByDescending(x => x.Position)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            CarryType = Dictionaries.GetItemById<CarryType>(_order.CarryId);
            NeedTrackNumber = !string.IsNullOrWhiteSpace(CarryType.TtnRegex);

            Title = "Заказ не забран";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            if (_order.WarehouseId != SelectedWarehouse!.Value && !MessageFacadeService.Confirm($"Склад отправки заказа \"{Warehouses?.FirstOrDefault(x => x.Id == _order.WarehouseId).ToString()}\" не соответствует складу возврата \"{Warehouses?.FirstOrDefault(x => x.Id == SelectedWarehouse.Value).ToString()}\". Продолжить?"))
            {
                return;
            }

            try
            {
                ExpireOrder request = new ExpireOrder(_order.Id, SelectedWarehouse!.Value, NeedTrackNumber ? TrackNumber : null, Comment);
                Task<Result<OrderDto>> resultTask = WebClient.ExecuteApiRequestAsync(request);
                Task<object> disassemblyServiceOptionTask = WebClient.ExecuteApiRequestAsync(new QueryDisassemblyServiceOption());

                await Task.WhenAll(resultTask, disassemblyServiceOptionTask);

                OrderDto resultOrder = resultTask.Result.Data;
                object disassemblyServiceOption = disassemblyServiceOptionTask.Result;

                if (disassemblyServiceOption is true)
                {
                    await OrderExpirePostProcessAsync(resultOrder.Id);
                }

                IsOk = true;
                Close();

                if (resultTask.Result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationInfo($"Заказ №{resultOrder.Id} обработан с предупреждениями");

                    ShowValidationResultView(
                        "Предупрежедения при обработке заказа",
                        resultTask.Result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Заказ №{resultOrder.Id} успешно обработан");
                }

                Messenger.Send(new OrderMessage(resultOrder, MessageType.Changed));
            }
            catch (UnexpectedSatusException exception)
            {
                ShowValidationResultView("Ошибки при обработке заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException)
            {
                ShowValidationResultView("Ошибки при обработке заказа", new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error while making order as taken");
                MessageFacadeService.ShowNotificationError("Ошибка при обработке заказа");
            }
        }

        private async Task OrderExpirePostProcessAsync(int orderExpireId)
        {
            AssemblyServicesFilteringItem assemblyFilter = new AssemblyServicesFilteringItem()
            {
                OrderIds = orderExpireId.ToString()
            };

            AdditionalServiceProductsFilteringItem additionalFilter = new AdditionalServiceProductsFilteringItem()
            {
                OrderIds = orderExpireId.ToString()
            };

            Task<PagedResult<AdditionalServiceProductDto>> additionalTask = WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProducts(additionalFilter));
            Task<PagedResult<AssemblyServiceDto>> assembliesTask = WebClient.ExecuteApiRequestAsync(new QueryAssemblyServices(assemblyFilter));

            await Task.WhenAll(additionalTask, assembliesTask);

            AdditionalServiceProductDto[] disassemblyAdditionalProducts = additionalTask.Result.Data?.Where(x => x.Disassembly).ToArray()
                                                               ?? Array.Empty<AdditionalServiceProductDto>();

            AssemblyServiceDto[] disassemblyAssemblyServices = assembliesTask.Result.Data?.Where(x => x.ProductId == null).ToArray()
                                                               ?? Array.Empty<AssemblyServiceDto>();

            if (disassemblyAssemblyServices.Any() || disassemblyAdditionalProducts.Any())
            {
                int times = 10;

                do
                {
                    await Task.Delay(3000);

                    if (disassemblyAssemblyServices.Any())
                    {
                        times = await PrintMovementAssemblyServiceAsync(disassemblyAssemblyServices, times);
                    }

                    await Task.Delay(3000);

                    if (disassemblyAdditionalProducts.Any())
                    {
                        times = await PrintAdditionalBarcodeAsync(disassemblyAdditionalProducts, times);
                    }

                    times--;
                }
                while (times > 0);
            }
        }

        private async Task<int> PrintMovementAssemblyServiceAsync(IReadOnlyCollection<AssemblyServiceDto> assemblyServices, int times)
        {
            int result = times;

            AssemblyServicesFilteringItem filterByParent = new AssemblyServicesFilteringItem
            {
                ParentIds = string.Join(',', assemblyServices.Select(x => x.Id.ToString()))
            };

            PagedResult<AssemblyServiceDto> assembliesByParent = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyServices(filterByParent));

            AssemblyServiceDto[] assemblyServicesByParents = assembliesByParent.Data.ToArray();

            foreach (AssemblyServiceDto item in assemblyServicesByParents)
            {
                AssemblyServiceDto parentAssemblyService = assemblyServices.FirstOrDefault(x => x.Id == item.ParentAssemblyServiceId);

                if (parentAssemblyService?.Places.HasValue == true)
                {
                    await ErrorHandler.HandleErrorsAsync(
                        _ => WebClient.ExecuteApiRequestAsync(new UpdateAssemblyServicePlaces(item.Id, parentAssemblyService.Places.Value)),
                        null,
                        null,
                        this,
                        false,
                        showDialog: false,
                        showError: false,
                        showNotification: false);
                }

                await Mediator.Send(new PrintMovementAssemblyServiceReportRequest(
                    item.Id,
                    item.OrderId,
                    parentAssemblyService?.Places ?? 1,
                    item.Products.Count(x => x.ProductId != Constants.AssemblyServiceProductId)));

                result = 0;
            }

            return result;
        }

        private async Task<int> PrintAdditionalBarcodeAsync(IReadOnlyCollection<AdditionalServiceProductDto> additionalProducts, int times)
        {
            int result = times;

            AdditionalServiceProductsFilteringItem additionalFilter = new AdditionalServiceProductsFilteringItem
            {
                ParentIds = string.Join(',', additionalProducts.Select(x => x.Id.ToString()))
            };

            PagedResult<AdditionalServiceProductDto> additionalServices = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProducts(additionalFilter));

            AdditionalServiceProductDto[] additionalServicesByParent = additionalServices.Data.ToArray();

            foreach (AdditionalServiceProductDto item in additionalServicesByParent)
            {
                await Mediator.Send(new PrintAdditionalServiceBarcodeReportRequest(
                    item.Id,
                    item.OrderId,
                    item.OrderDeliveryTimeTo,
                    item.Date));

                result = 0;
            }

            return result;
        }
    }
}