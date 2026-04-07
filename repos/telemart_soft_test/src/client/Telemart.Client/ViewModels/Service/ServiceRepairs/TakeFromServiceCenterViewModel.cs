using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Order;
using Telemart.Client.Data.Requests.Features.ServiceRepair.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceRepairs
{
    internal sealed class TakeFromServiceCenterViewModel : TelemartDialogViewModelBase
    {
        public const int MaxReasonLength = 100;
        public const int MaxAltLength = 100;
        public const int MaxActLength = 30;
        public const int MaxConclusionLength = 1000;

        private int repairId;

        public TakeFromServiceCenterViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;
        }

        public TakeFromServiceCenterViewModel()
        {
        }

        #region INPC

        public int? WarehouseLocationId
        {
            get { return GetProperty(() => WarehouseLocationId); }
            set { SetProperty(() => WarehouseLocationId, value); }
        }

        public ServiceRepairState RepairState
        {
            get { return GetProperty(() => RepairState); }
            set { SetProperty(() => RepairState, value, () => { RaisePropertiesChanged(nameof(RejectReason), nameof(RejectAlternative), nameof(Act)); }); }
        }

        public string RejectReason
        {
            get { return GetProperty(() => RejectReason); }
            set { SetProperty(() => RejectReason, value); }
        }

        public string RejectAlternative
        {
            get { return GetProperty(() => RejectAlternative); }
            set { SetProperty(() => RejectAlternative, value); }
        }

        public bool WarrantyRemove
        {
            get { return GetProperty(() => WarrantyRemove); }
            set { SetProperty(() => WarrantyRemove, value); }
        }

        public bool WarrantyRemoveVisible
        {
            get { return GetProperty(() => WarrantyRemoveVisible); }
            set { SetProperty(() => WarrantyRemoveVisible, value); }
        }

        public string Act
        {
            get { return GetProperty(() => Act); }
            set { SetProperty(() => Act, value); }
        }

        public string ServiceCenterConclusion
        {
            get { return GetProperty(() => ServiceCenterConclusion); }
            set { SetProperty(() => ServiceCenterConclusion, value); }
        }

        public ReadOnlyObservableCollection<ValidatableItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        #endregion

        private IMessenger Messenger { get; }

        public static void BuildMetadata(MetadataBuilder<TakeFromServiceCenterViewModel> builder)
        {
            builder.Property(x => x.WarehouseLocationId)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.RepairState)
                .Required(() => Resources.RequiredErrorMessage);

            builder.Property(x => x.RejectReason)
                .MatchesInstanceRule(
                    (x, y) => y.RepairState != ServiceRepairState.DenyOfWarranty || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.RejectAlternative)
                .MatchesInstanceRule(
                    (x, y) => y.RepairState != ServiceRepairState.DenyOfWarranty || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.Act)
                .MatchesInstanceRule(
                    (x, y) => y.RepairState != ServiceRepairState.RemovedFromRegister || !string.IsNullOrWhiteSpace(x),
                    () => Resources.RequiredErrorMessage);

            builder.Property(x => x.ServiceCenterConclusion)
                .MatchesRule(
                    (x) => string.IsNullOrWhiteSpace(x) || x.Length <= 1000,
                    () => "Количество символов должно быть меньше или равна 1000");
        }

        protected override async Task HandleLoadedAsync()
        {
            ServiceRepairViewItem repair = (ServiceRepairViewItem)Parameter;

            OrderDto order = await WebClient.ExecuteApiRequestAsync(new QueryOrder(repair.ServiceRequestOrderId));

            if (order.Products.FirstOrDefault(x => x.Product.Id == repair.ProductId)?.SerialNumbers.Any() == true)
            {
                WarrantyRemoveVisible = true;
            }

            await RefreshWarehousesAsync();

            repairId = repair.Id;
            WarehouseLocationId = repair.ServiceRequestWarehouseLocationId;

            Title = "Принять из СЦ";
        }

        protected override async Task HandleOkAsync()
        {
            if (IDataErrorInfoHelper.HasErrors(this))
            {
                return;
            }

            DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>(
                $"Вы подтверждаете результат \"{RepairState.Name}\"",
                this);

            if (!viewModel.IsOk)
            {
                return;
            }

            try
            {
                TakeFromServiceCenter gatewayRequest = new TakeFromServiceCenter(repairId, BuildDto());

                Result<ServiceRepairDto> result = await WebClient.ExecuteApiRequestAsync(gatewayRequest);

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning($"Ремонт №{repairId} принят из СЦ c предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToArray());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo($"Ремонт №{repairId} успешно принят из СЦ");
                }

                Messenger.Send(new ServiceRepairMessage(result.Data, MessageType.Changed));
                Messenger.Send(new ServiceRepairWorkflowMessage(result.Data));

                IsOk = true;
                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при принятии ремонта из СЦ");
                ShowValidationResultView("Ошибки при принятии ремонта из СЦ", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to take service request");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при принятии ремонта из СЦ");
                Logger.LogError(exception, "Failed to take service request");
            }
        }

        private async Task RefreshWarehousesAsync()
        {
            List<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();

            Warehouses = warehouses
                .Where(x => x.Active == 1 && WebClient.AuthenticatedEmployee.AllowWarehouses.Contains(x.Id))
                .OrderByDescending(x => x.Position)
                .ThenBy(x => x.Name)
                .Select(x => new ValidatableItem { Id = x.Id, Name = x.Name })
                .ToReadOnlyObservableCollection();
        }

        private ServiceRepairTakeFromServiceCenterDto BuildDto()
        {
            ServiceRepairTakeFromServiceCenterDto dto = new ServiceRepairTakeFromServiceCenterDto
            {
                Id = repairId,
                WarehouseLocationId = WarehouseLocationId.Value,
                StateId = RepairState.Id,
                ServiceCenterConclusion = this.ServiceCenterConclusion
            };

            if (RepairState == ServiceRepairState.DenyOfWarranty)
            {
                dto.Reason = RejectReason;
                dto.Alternative = RejectAlternative;
                dto.WarrantyRemove = WarrantyRemove;
            }
            else if (RepairState == ServiceRepairState.RemovedFromRegister)
            {
                dto.Act = Act;
            }

            return dto;
        }
    }
}