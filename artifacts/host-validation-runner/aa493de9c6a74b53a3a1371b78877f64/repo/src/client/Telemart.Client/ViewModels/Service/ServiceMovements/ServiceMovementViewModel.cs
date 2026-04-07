using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Calculators;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Carry;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Novaposhta.Actions;
using Telemart.Client.Data.Requests.Features.ServiceMovement;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Mediator.Requests;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.ServiceMovement;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common.RecognizeServiceBarcode;
using Telemart.Client.ViewModels.Store;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceMovements
{
    public class ServiceMovementViewModel : TelemartEditorViewModelBase<ServiceMovementDto, ServiceMovementParameter, ServiceMovementViewItem>
    {
        private IReadOnlyDictionary<int, string> warehouseNamesDictionary;
        private IReadOnlyDictionary<int, string> employeeNamesDictionary;
        private IReadOnlyDictionary<int, string> deliveryTypes;
        private IReadOnlyDictionary<int, string> carryTypesDictionary;

        public ServiceMovementViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IMediator mediator,
            IInsuranceCalculator calculatorInsurance,
            RecognizeServiceBarcodeViewModel recognizeServiceBarcodeViewModel)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            DeleteProductCommand = new AsyncCommand(DeleteProductAsync, () => Model?.EmployeeLockId == null && SelectedProduct != null);
            CancelMovementCommand = new AsyncCommand(CancelMovementAsync, () => Model?.EmployeeLockId == null);
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickEventArgs>(HandleRowDoubleClick);
            CreateNpDocumentCommand = new AsyncCommand(CreateNpDocumentTtnAsync, () => Model?.EmployeeLockId == null);

            RecognizeServiceBarcodeViewModel = recognizeServiceBarcodeViewModel;

            RecognizeServiceBarcodeViewModel.OnFinished += RecognizeServiceBarcodeViewModelOnFinished;

            Mediator = mediator;
            ErrorHandler = errorHandler;
            CalculatorInsurance = calculatorInsurance;
        }

        public RecognizeServiceBarcodeViewModel RecognizeServiceBarcodeViewModel { get; }

        #region Commands

        public IAsyncCommand DeleteProductCommand { get; }

        public IAsyncCommand CancelMovementCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IAsyncCommand CreateNpDocumentCommand { get; }

        private IMediator Mediator { get; }

        private IErrorHandler ErrorHandler { get; }

        private IInsuranceCalculator CalculatorInsurance { get; }

        #endregion

        #region INPC

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ServiceMovementProductViewItem SelectedProduct
        {
            get { return GetProperty(() => SelectedProduct); }
            set { SetProperty(() => SelectedProduct, value); }
        }

        public bool VerifyCreateNpTtn
        {
            get { return GetProperty(() => VerifyCreateNpTtn); }
            set { SetProperty(() => VerifyCreateNpTtn, value); }
        }

        public bool ButtonsIsVisible => Model?.State == MovementState.Left || Model?.State == MovementState.Arrived;

        public bool VerifyButtonIsVisible => !IsLockedByCurrentEmployee && ButtonsIsVisible && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(Model?.WarehouseToId ?? 0) && Model?.Products?.Any() == true;

        public bool ReceiveButtonIsVisible => IsLockedByCurrentEmployee && ButtonsIsVisible && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(Model?.WarehouseToId ?? 0) && Model?.Products?.Any() == true;

        public bool CancelButtonIsVisible => Model?.EmployeeLockId == null && Model?.State != MovementState.Cancelled && Model?.State != MovementState.Received && WebClient.IsOperationAllowed(BusinessOperation.ServiceMovementCancel);

        #endregion

        public override int Width => 860;

        public override int Height => 500;

        public override int MinWidth => 600;

        public override int MinHeight => 400;

        protected override string CreatedActionMessage => "создано";

        protected override string EntityName => "Перемещение";

        protected override string UpdatedActionMessage => "сохранено";

        protected override Task<Result<ServiceMovementDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(ServiceMovementDto taskDto, MessageType messageType)
        {
            return new ServiceMovementMessage(taskDto, messageType);
        }

        protected override Task<ServiceMovementDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryServiceMovement(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            ServiceMovementParameter parameter = (ServiceMovementParameter)Parameter;

            carryTypesDictionary = Dictionaries.GetItems<CarryType>().ToDictionary(x => x.Id, x => x.Name);

            if (parameter.IsNew)
            {
                throw new NotSupportedException("Service movement creation is not supported");
            }

            await Task.WhenAll(RefreshEmployees(), RefreshWarehouses(), RefreshDelivaryType());

            await base.HandleLoadedAsync();

            async Task RefreshEmployees()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

                employeeNamesDictionary = employees.ToDictionary(x => x.Id, y => y.Name);
            }

            async Task RefreshWarehouses()
            {
                List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

                warehouseNamesDictionary = warehouses.ToDictionary(x => x.Id, y => y.Name);
            }

            async Task RefreshDelivaryType()
            {
                List<DeliveryTypeDto> allDeliveryTypes = await WebClient.ExecuteApiRequestAsync(new QueryDeliveryTypes());

                deliveryTypes = allDeliveryTypes.ToDictionary(x => x.Id, y => y.Name);
            }
        }

        protected override void AfterSetData()
        {
            Model.Products.ForEach(x => x.MovementState = Model.State);
            ModelOriginal.Products.ForEach(x => x.MovementState = ModelOriginal.State);

            Model.Products = Model.Products
                .OrderBy(x => x.ProductFullName)
                .ThenBy(x => x.ServiceRequestId)
                .ToObservableCollection();

            ModelOriginal.Products = ModelOriginal.Products
                .OrderBy(x => x.ProductFullName)
                .ThenBy(x => x.ServiceRequestId)
                .ToObservableCollection();

            VerifyCreateNpTtn = Model?.State == MovementState.Left && string.IsNullOrEmpty(Model?.TrackNumber) && (Model?.CarryId == CarryType.NpDeliveryId || Model?.CarryId == CarryType.NpWarehouseId);

            RefreshSummaryItems();
            RaiseProperties();
        }

        protected override Task<LockResponse<ServiceMovementDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockServiceMovement(id, checkPermissions: true));
        }

        protected override Task<LockResponse<ServiceMovementDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockServiceMovement(id));
        }

        protected override void SetCreateTitle()
        {
        }

        protected override void SetEditTitle()
        {
            Title = $"Сервисное перемещение №{Model.Id}";
        }

        protected override Task<bool> SaveAsync()
        {
            if (Model.Products.Any(x => !x.IsProcessed && !x.Received) &&
                !ShowValidationResultView("Предупреждение", new[] { new ValidationResultItem("Отсканированы не все заявки", false) }))
            {
                return Task.FromResult(false);
            }

            return base.SaveAsync();
        }

        protected override Task<Result<ServiceMovementDto>> UpdateEntityAsync()
        {
            int[] productIds = Model.Products.Where(x => x.IsProcessed && !x.Received).Select(x => x.Id).ToArray();
            return WebClient.ExecuteApiRequestAsync(new ReceiveServiceMovement(Model.Id, new ServiceMovementReceiveDto { ProductIds = productIds }));
        }

        private async Task DeleteProductAsync()
        {
            if (!MessageFacadeService.Confirm("Вы уверены?"))
            {
                return;
            }

            try
            {
                await WebClient.ExecuteApiRequestAsync(new ServiceMovementDeleteProduct(Model.Id, SelectedProduct.Id));

                Model.Products.Remove(SelectedProduct);
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении товара из перемещения");
                ShowValidationResultView("Ошибки при удалении товара из перемещения", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to delete service movement product");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при удалении товара из перемещения");
                Logger.LogError(exception, "Error while deleting service movement product");
            }
        }

        private Task CancelMovementAsync()
        {
            return MessageFacadeService.Confirm("Вы действительно хотите отменить сервисное перемещение?")
                ? ExecuteLockableOperationAsync(x => WebClient.ExecuteApiRequestAsync(new CancelServiceMovement(x.Id)))
                : Task.CompletedTask;
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems();

            IEnumerable<SummaryViewItem> GetSummaryItems()
            {
                const string Format = DateFormattingRules.FullDateTimeFormat;

                yield return new SummaryViewItem("Номер", $"{Model.Id}");
                yield return new SummaryViewItem("Отправитель", $"{warehouseNamesDictionary.GetValueOrDefault(Model.WarehouseFromId)}");
                yield return new SummaryViewItem("Получатель", $"{warehouseNamesDictionary.GetValueOrDefault(Model.WarehouseToId)}");
                yield return new SummaryViewItem("Статус", $"{Model.State.Name}");
                yield return new SummaryViewItem("Создал", $"{employeeNamesDictionary.GetValueOrDefault(Model.CreatedBy)} ({Model.CreatedOn.ToString(Format)})");
                yield return new SummaryViewItem("Принято", $"{Model.Products.Count(x => x.Received)} / {Model.Products.Count}");

                if (Model.ReceivedBy.HasValue && Model.ReceivedOn.HasValue)
                {
                    yield return new SummaryViewItem("Принял", $"{employeeNamesDictionary.GetValueOrDefault(Model.ReceivedBy.Value)} ({Model.ReceivedOn.Value.ToString(Format)})");
                }

                if (Model.CarryId is not null && carryTypesDictionary.TryGetValue(Model.CarryId.Value, out string name))
                {
                    yield return new SummaryViewItem("Доставка", $"{name}");
                }

                if (Model.DeliveryTypeId is not null)
                {
                    yield return new SummaryViewItem("Тип доставки", $"{deliveryTypes.GetValueOrDefault(Model.DeliveryTypeId.Value)}");
                }

                if (Model.Places is not null)
                {
                    yield return new SummaryViewItem("Мест", $"{Model.Places}");
                }

                if (!string.IsNullOrEmpty(Model.TrackNumber))
                {
                    yield return new SummaryViewItem("ТТН", $"{Model.TrackNumber}");
                }
            }
        }

        private void RecognizeServiceBarcodeViewModelOnFinished(object sender, RecognizeServiceBarcodeResultEventArgs e)
        {
            if (e.IsValid && e.ServiceRequestId.HasValue)
            {
                ServiceMovementProductViewItem product = Model.Products.FirstOrDefault(x => x.ServiceRequestId == e.ServiceRequestId.Value);

                if (product != null)
                {
                    product.IsProcessed = true;
                }
                else
                {
                    MessageFacadeService.ShowNotificationWarning("Сервисной заявки нет в перемещении");
                }
            }
            else
            {
                MessageFacadeService.ShowNotificationWarning(e.ErrorText, true);
            }
        }

        private void HandleRowDoubleClick(RowDoubleClickEventArgs args)
        {
            ServiceMovementProductViewItem item = (ServiceMovementProductViewItem)((GridControl)args.Source.DataControl).CurrentItem;

            switch (args.HitInfo.Column.FieldName)
            {
                case nameof(item.ServiceRequestId):
                    ShowServiceRequest(item.ServiceRequestId);
                    break;
            }
        }

        private void ShowServiceRequest(int id)
        {
            Messenger.Send(new ServiceRequestViewMessage(id));
        }

        private void RaiseProperties()
        {
            RaisePropertiesChanged(nameof(ButtonsIsVisible), nameof(VerifyButtonIsVisible), nameof(ReceiveButtonIsVisible), nameof(CancelButtonIsVisible));
        }

        private async Task CreateNpDocumentTtnAsync()
        {
            decimal insurance = await CalculatorInsurance.CalculateByServiceMovementAsync(Model.Products.ToArray());
            PackagePropertiesViewModel packagePropertiesViewModel = GetPackageProperties(insurance);

            if (!packagePropertiesViewModel.IsOk)
            {
                return;
            }

            int places = (int)packagePropertiesViewModel.PackagePlaces;

            Result<ServiceMovementNpDocumentDto> result = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateNpTtnByServiceMovement(Model.Id, places, (double)packagePropertiesViewModel.TotalWeight, packagePropertiesViewModel.Insurance, !packagePropertiesViewModel.NotAddToNpApplication)),
                "при создании ТТН",
                "ТТН создана",
                this,
                true);

            if (result?.Data is null)
            {
                return;
            }

            try
            {
                Messenger.Send(new ServiceMovementMessage(result.Data, MessageType.Changed));

                await Mediator.Send(new PrintTrackNumberRequest(result.Data.TrackNumber, result.Data.Link, true));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to print ttn for service movement");
                MessageFacadeService.ShowNotificationError("Ошибка при печати ТТН. Попробуйте снова");
            }
            finally
            {
                CloseOk();
            }
        }

        private PackagePropertiesViewModel GetPackageProperties(decimal insurance)
        {
            return SizeableDialogDocumentManagerService.ShowView<PackagePropertiesViewModel>(new PackagePropertiesParameter(0, insurance, Model.CarryId!.Value), this);
        }
    }
}