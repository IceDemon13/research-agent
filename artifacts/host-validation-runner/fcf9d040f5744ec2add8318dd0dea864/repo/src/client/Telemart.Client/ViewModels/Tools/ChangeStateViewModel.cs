using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.AdditionalServiceProduct;
using Telemart.Client.Data.Requests.Features.AssemblyService;
using Telemart.Client.Data.Requests.Features.Call;
using Telemart.Client.Data.Requests.Features.ExternalPayment;
using Telemart.Client.Data.Requests.Features.Invoice;
using Telemart.Client.Data.Requests.Features.Movement;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.Refund;
using Telemart.Client.Data.Requests.Features.ReturnInvoice;
using Telemart.Client.Data.Requests.Features.ServiceRequest;
using Telemart.Client.Data.Requests.Features.SupplierBill;
using Telemart.Client.Data.Requests.Features.Tools;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.AssemblyService;
using Telemart.Client.TransferObjects.Call;
using Telemart.Client.TransferObjects.ReturnInvoice;
using Telemart.Client.TransferObjects.SupplierBill;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Tools
{
    public sealed class ChangeStateViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private IReadOnlyCollection<EntityChangeStateDto> _entityChangeStates;

        public ChangeStateViewModel(
            IWebClient webClient,
            IErrorHandler errorHandler,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        public int? DocumentId
        {
            get { return GetProperty(() => DocumentId); }
            set { SetProperty(() => DocumentId, value); }
        }

        public int? SelectedEntityId
        {
            get { return GetProperty(() => SelectedEntityId); }
            set { SetProperty(() => SelectedEntityId, value, OnSelectedEntityIdChanged); }
        }

        public int? SelectedStateId
        {
            get { return GetProperty(() => SelectedStateId); }
            set { SetProperty(() => SelectedStateId, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Entities
        {
            get { return GetProperty(() => Entities); }
            private set { SetProperty(() => Entities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> States
        {
            get { return GetProperty(() => States); }
            private set { SetProperty(() => States, value); }
        }

        public static void BuildMetadata(MetadataBuilder<ChangeStateViewModel> builder)
        {
            builder.Property(x => x.SelectedEntityId).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.DocumentId).MatchesRule(x => x > 0, () => "Значение должно быть больше нуля");
            builder.Property(x => x.SelectedStateId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            _entityChangeStates = await WebClient.ExecuteApiRequestAsync(new QueryEntityChangeStates());

            HashSet<int> allowedEntityIds = _entityChangeStates.Select(x => x.EntityId).ToHashSet();

            Entities = Dictionaries
                .GetItems<Entity>()
                .Where(x => allowedEntityIds.Contains(x.Id))
                .Select(x => new ComboBoxItem(x.Id, x.DisplayName))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();

            Title = "Изменения статуса";

            await base.HandleLoadedAsync();
        }

        protected override async Task HandleOkAsync()
        {
            int? fromStateId = null;

            try
            {
                switch (SelectedEntityId!.Value)
                {
                    case Entity.MovementId:

                        MovementDto movement = await WebClient.ExecuteApiRequestAsync(new QueryMovement(DocumentId!.Value));

                        fromStateId = movement.StateId;

                        break;

                    case Entity.OrderId:

                        OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(DocumentId!.Value));

                        fromStateId = order.StateId;

                        break;

                    case Entity.InvoiceId:

                        InvoiceDto invoice = await WebClient.ExecuteApiRequestAsync(new QueryInvoice(DocumentId!.Value));

                        fromStateId = invoice.StateId;

                        break;

                    case Entity.AssemblyServiceId:

                        AssemblyServiceDto assemblyService = await WebClient.ExecuteApiRequestAsync(new QueryAssemblyService(DocumentId!.Value));

                        fromStateId = assemblyService.StateId;

                        break;

                    case Entity.RefundId:

                        RefundDto refund = await WebClient.ExecuteApiRequestAsync(new QueryRefund(DocumentId!.Value));

                        fromStateId = refund.StateId;

                        break;

                    case Entity.ReturnInvoiceId:

                        ReturnInvoiceDto returnInvoice = await WebClient.ExecuteApiRequestAsync(new QueryReturnInvoice(DocumentId!.Value));

                        fromStateId = returnInvoice.StateId;

                        break;

                    case Entity.ServiceRequestId:

                        ServiceRequestDto serviceRequest = await WebClient.ExecuteApiRequestAsync(new QueryServiceRequest(DocumentId!.Value));

                        fromStateId = serviceRequest.StateId;

                        break;

                    case Entity.CallId:

                        CallDto call = await WebClient.ExecuteApiRequestAsync(new QueryCall(DocumentId!.Value));

                        fromStateId = call.StateId;

                        break;

                    case Entity.TradeInId:

                        TradeInDto tradeIn = await WebClient.ExecuteApiRequestAsync(new QueryTradeIn(DocumentId!.Value));

                        fromStateId = tradeIn.StateId;

                        break;

                    case Entity.AdditionalServiceProductId:

                        AdditionalServiceProductDto additionalServiceProduct = await WebClient.ExecuteApiRequestAsync(new QueryAdditionalServiceProduct(DocumentId!.Value));

                        fromStateId = additionalServiceProduct.StateId;

                        break;

                    case Entity.SupplierBillId:

                        SupplierBillDto supplierBill = await WebClient.ExecuteApiRequestAsync(new QuerySupplierBill(DocumentId!.Value));

                        fromStateId = supplierBill.StateId;

                        break;

                    case Entity.ExternalPaymentId:

                        ExternalPaymentDto externalPayment = await WebClient.ExecuteApiRequestAsync(new QueryExternalPayment(DocumentId!.Value));

                        fromStateId = externalPayment.PaymentStateId;

                        break;
                }
            }
            catch (UnexpectedSatusException ex) when (ex.Message.Contains("Resource not found", StringComparison.OrdinalIgnoreCase))
            {
                MessageFacadeService.ShowNotificationError("Документ не найден");
                return;
            }
            catch (Exception ex)
            {
                MessageFacadeService.ShowNotificationError("Не удалось запросить документ");
                Logger.LogError(ex, "Failed to query document for state changing");

                return;
            }

            if (fromStateId == SelectedStateId)
            {
                MessageFacadeService.ShowNotificationWarning("Документ уже находится в этом статусе");
                return;
            }

            EntityChangeStateDto changeEntityState = _entityChangeStates.FirstOrDefault(x => x.EntityId == SelectedEntityId!.Value && x.FromStateId == fromStateId && x.ToStateId == SelectedStateId!.Value);

            if (changeEntityState is null)
            {
                MessageFacadeService.ShowNotificationError("Выбранное изменение статуса не доступно");
                return;
            }

            string confirmText = string.IsNullOrWhiteSpace(changeEntityState.Info) ? "Вы уверены?" : changeEntityState.Info;

            if (!MessageFacadeService.Confirm(confirmText))
            {
                return;
            }

            ChangeStateDto dto = new ChangeStateDto()
            {
                EntityId = SelectedEntityId!.Value,
                StateId = SelectedStateId!.Value,
                DocumentId = DocumentId!.Value
            };

            Result<object> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new ChangeState(dto)),
                "изменении статуса",
                "Статус изменен",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                CloseOk();
            }
        }

        private void OnSelectedEntityIdChanged()
        {
            if (SelectedEntityId is null)
            {
                SelectedStateId = null;
            }

            States = SelectedEntityId switch
            {
                Entity.OrderId => Dictionaries.GetItems<OrderStatus>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.RefundId => Dictionaries.GetItems<RefundState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.ServiceRequestId => Dictionaries.GetItems<ServiceRequestState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.TradeInId => Dictionaries.GetItems<TradeInState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.CallId => Dictionaries.GetItems<CallState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.AssemblyServiceId => Dictionaries.GetItems<AssemblyServiceState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.AdditionalServiceProductId => Dictionaries.GetItems<AdditionalServiceProductState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.SupplierBillId => Dictionaries.GetItems<SupplierBillState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.InvoiceId => Dictionaries.GetItems<InvoiceState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.ReturnInvoiceId => Dictionaries.GetItems<ReturnInvoiceState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.MovementId => Dictionaries.GetItems<MovementState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                Entity.ExternalPaymentId => Dictionaries.GetItems<PaymentState>().Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection(),
                _ => new ReadOnlyObservableCollection<ComboBoxItem>(new ObservableCollection<ComboBoxItem>())
            };
        }
    }
}