using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Business.Order;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Products;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Nomenclature;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order
{
    public sealed class CreatePresaleOrderViewModel : TelemartDialogViewModelBase
    {
        public CreatePresaleOrderViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IOrderRules orderRules)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
            OrderRules = orderRules;
            SelectProductCommand = new DelegateCommand(SelectProduct);
        }

        public CreatePresaleOrderViewModel()
        {
        }

        #region Commands

        public IDelegateCommand SelectProductCommand { get; }

        #endregion

        #region INPC

        public ProductItem Product
        {
            get
            {
                return GetProperty(() => Product);
            }

            set
            {
                SetProperty(() => Product, value, ChangedCallback);

                void ChangedCallback()
                {
                    SerialNumber = null;
                    RaisePropertyChanged(nameof(SerialNumber));
                }
            }
        }

        public string SerialNumber
        {
            get { return GetProperty(() => SerialNumber); }
            set { SetProperty(() => SerialNumber, value); }
        }

        public int? WarehouseId
        {
            get { return GetProperty(() => WarehouseId); }
            set { SetProperty(() => WarehouseId, value); }
        }

        public int? ContractorId
        {
            get { return GetProperty(() => ContractorId); }
            set { SetProperty(() => ContractorId, value); }
        }

        public bool CreateServiceRequest
        {
            get { return GetProperty(() => CreateServiceRequest); }
            set { SetProperty(() => CreateServiceRequest, value, () => RaisePropertyChanged(nameof(StatedDefect))); }
        }

        public string StatedDefect
        {
            get { return GetProperty(() => StatedDefect); }
            set { SetProperty(() => StatedDefect, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public ReadOnlyObservableCollection<ContractorDto> Contractors
        {
            get { return GetProperty(() => Contractors); }
            private set { SetProperty(() => Contractors, value); }
        }

        #endregion

        public bool ReadOnlyCreateServiceRequest => WebClient.IsOperationAllowed(BusinessOperation.PresaleOrderCreateServiceRequest);

        private IMessenger Messenger { get; }

        private IOrderRules OrderRules { get; }

        public static void BuildMetadata(MetadataBuilder<CreatePresaleOrderViewModel> builder)
        {
            builder.Property(x => x.Product)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.SerialNumber)
                .MatchesInstanceRule((x, y) => y.Product == null || !y.Product.KeepSerial || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);

            builder.Property(x => x.WarehouseId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.ContractorId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.StatedDefect)
                .MatchesInstanceRule((x, y) => !y.CreateServiceRequest || !string.IsNullOrWhiteSpace(x), () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            Warehouses = warehouses.Data
                .Where(x => x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderByDescending(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            PagedResult<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true);

            Contractors = contractors.Data
                .Where(x => x.Active
                     && !x.IsFolder
                     && x.IsClient
                     && WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(x.SubdivisionId)
                     && x.ParentId == Constants.ServiceContractorId)
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            CreateServiceRequest = true;

            Title = "Списание";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            string errorMessage = ValidateSerialNumber();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                MessageFacadeService.ShowNotificationError(errorMessage, true);
                return;
            }

            try
            {
                OrderCreatePresaleDto dto = new OrderCreatePresaleDto
                {
                    ContractorId = ContractorId.Value,
                    WarehouseId = WarehouseId.Value,
                    ProductId = Product.Id,
                    SerialNumber = Product.KeepSerial
                        ? SerialNumber
                        : null,
                    StatedDefect = CreateServiceRequest && !string.IsNullOrWhiteSpace(StatedDefect)
                        ? StatedDefect
                        : null,
                    OrderSourceId = OrderSourceType.InnerOrder
                };

                Result<OrderCreatePresaleResponse> result = await WebClient.ExecuteApiRequestAsync(new CreatePresaleOrder(dto));

                OrderDto order = result.Data.Order;
                ServiceRequestDto serviceRequest = result.Data.ServiceRequest;

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationInfo($"Заказ №{order.Id} создан с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Заказ №{order.Id} успешно создан");
                }

                if (serviceRequest != null)
                {
                    Messenger.Send(new ServiceRequestMessage(serviceRequest, MessageType.Added));
                }

                Messenger.Send(new OrderMessage(order, MessageType.Added));
                Messenger.Send(new OrderEditViewMessage(order.Id));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании заказа");
                ShowValidationResultView("Ошибки при создании заказа", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create presale order");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании заказа");
                Logger.LogError(exception, "Failed to create presale order");
            }
        }

        private void SelectProduct()
        {
            NomenclatureViewOptions options = new NomenclatureViewOptions(
                NomenclatureViewPriceContext.Client,
                Constants.TelemartContractorId,
                NomenclatureViewSelectionMode.Single,
                false);

            NomenclatureViewModel nomenclatureViewModel = SizeableDialogDocumentManagerService.ShowView<NomenclatureViewModel>(options, this);

            if (nomenclatureViewModel.IsOk)
            {
                NomenclatureViewItem product = nomenclatureViewModel.GetSelectedItems().First();

                ProductAttributesDto productAttributes = WebClient.ExecuteApiRequest(new QueryProductAttributes(product.Id));

                Product = new ProductItem
                {
                    Id = product.Id,
                    Name = product.Name,
                    KeepSerial = productAttributes.KeepSerial,
                    SerialNumberLength = productAttributes.SerialNumberLength
                };
            }
        }

        private string ValidateSerialNumber()
        {
            string errorMessage = Product.KeepSerial
                ? OrderRules.ValidateSerialNumber(SerialNumber, Product.SerialNumberLength)
                : null;

            return errorMessage;
        }

        public class ProductItem : BindableBase
        {
            public int Id
            {
                get { return GetProperty(() => Id); }
                set { SetProperty(() => Id, value); }
            }

            public string Name
            {
                get { return GetProperty(() => Name); }
                set { SetProperty(() => Name, value); }
            }

            public bool KeepSerial
            {
                get { return GetProperty(() => KeepSerial); }
                set { SetProperty(() => KeepSerial, value, () => { RaisePropertyChanged(nameof(SerialNumber)); }); }
            }

            public List<ProductSnLengthDto> SerialNumberLength
            {
                get { return GetProperty(() => SerialNumberLength); }
                set { SetProperty(() => SerialNumberLength, value); }
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}