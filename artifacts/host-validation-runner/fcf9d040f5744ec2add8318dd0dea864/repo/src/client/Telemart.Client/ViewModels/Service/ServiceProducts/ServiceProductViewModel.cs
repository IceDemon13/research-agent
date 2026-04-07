using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Business;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Contractor;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.ServiceProduct;
using Telemart.Client.Data.Requests.Features.ServiceProduct.Actions;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.ServiceProduct;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class ServiceProductViewModel : TelemartEditorExViewModelBase<ServiceProductDto, ServiceProductParameter, ServiceProductViewItem>
    {
        private IReadOnlyDictionary<int, string> employees;
        private IReadOnlyDictionary<int, WarehouseDto> warehouses;

        public ServiceProductViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger,
            DocumentCommands documentCommands)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            DocumentCommands = documentCommands;
            DiscountCommand = new AsyncCommand(DiscountAsync);
            PutOnWarehouseCommand = new AsyncCommand(PutOnWarehouseAsync);
            GiveBackToSupplierCommand = new AsyncCommand(GiveBackToSupplierAsync);
            OnUtilizationCommand = new AsyncCommand(OnUtilizationAsync);
            UtilizeCommand = new AsyncCommand(UtilizeAsync, () => Model?.State == ServiceProductState.OnUtilization && WebClient.IsOperationAllowed(BusinessOperation.ServiceProductUtilize));
            GiveOnRepairCommand = new AsyncCommand(GiveOnRepairAsync);
            SupplierRemoveFromRegisterCommand = new AsyncCommand(SupplerRemoveFromRegisterAsync);
            SupplierChangeCommand = new AsyncCommand(SupplierChangeAsync);
            SupplierRejectCommand = new AsyncCommand(SupplierRejectAsync);
            MoveCommand = new AsyncCommand(MoveAsync);
            ChangingDecisionCommand = new AsyncCommand(ChangeDecisionAsync, CanChangingDecision);

            OpenServiceRequestCommand = new DelegateCommand<int>(OpenServiceRequest);
            OpenServiceRepairCommand = new DelegateCommand<int?>(OpenServiceRepair, x => x != null);
        }

        public ServiceProductViewModel()
        {
        }

        #region Commands

        public IAsyncCommand DiscountCommand { get; }

        public IAsyncCommand MoveCommand { get; }

        public IAsyncCommand PutOnWarehouseCommand { get; }

        public IAsyncCommand GiveBackToSupplierCommand { get; }

        public IAsyncCommand UtilizeCommand { get; }

        public IAsyncCommand OnUtilizationCommand { get; }

        public IAsyncCommand GiveOnRepairCommand { get; }

        public IAsyncCommand SupplierChangeCommand { get; }

        public IAsyncCommand SupplierRemoveFromRegisterCommand { get; }

        public IAsyncCommand SupplierRejectCommand { get; }

        public IDelegateCommand OpenServiceRequestCommand { get; }

        public IDelegateCommand OpenServiceRepairCommand { get; }

        public DocumentCommands DocumentCommands { get; }

        public IAsyncCommand ChangingDecisionCommand { get; }

        #endregion

        #region INPC

        public IEnumerable<SummaryViewItem> SummaryItems
        {
            get { return GetProperty(() => SummaryItems); }
            private set { SetProperty(() => SummaryItems, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Suppliers
        {
            get { return GetProperty(() => Suppliers); }
            private set { SetProperty(() => Suppliers, value); }
        }

        public bool CanProcess => Model != null && Model.EmployeeLockId == null && Model.State == ServiceProductState.New;

        public bool IsTradeInProduct => Model != null && Model.TypeId == ServiceProductType.TradeIn.Id;

        public bool CanProcessFromSupplier => Model != null && Model.EmployeeLockId == null && Model.State == ServiceProductState.TransferToSupplier;

        #endregion

        protected override string CreatedActionMessage => "создан";

        protected override string EntityName => "Сервисный товар";

        protected override string UpdatedActionMessage => "сохранен";

        protected override Task<Result<ServiceProductDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override object CreateEntityMessage(ServiceProductDto taskDto, MessageType messageType)
        {
            return new ServiceProductMessage(taskDto, messageType);
        }

        protected override Task<ServiceProductDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryServiceProduct(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            ServiceProductParameter parameter = (ServiceProductParameter)Parameter;

            if (parameter.IsNew)
            {
                throw new NotSupportedException("Service product creation is not supported");
            }

            await Task.WhenAll(RefreshEmployees(), RefreshWarehouses());

            await base.HandleLoadedAsync();

            await RefreshSuppliers();

            async Task RefreshEmployees()
            {
                PagedResult<EmployeeDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryEmployees());
                employees = pagedResult.Data.ToDictionary(x => x.Id, y => y.Name);
            }

            async Task RefreshSuppliers()
            {
                List<ContractorDto> contractors = await WebClient.ExecuteApiRequestAsync(new QueryContractors(), true).GetPagedResultDataAsync();
                Suppliers = contractors
                    .Where(x => x.IsSupplier && !x.IsFolder)
                    .OrderBy(x => x.Name)
                    .Select(x => new ComboBoxItem(x.Id, x.Name))
                    .ToReadOnlyObservableCollection();
            }

            async Task RefreshWarehouses()
            {
                List<WarehouseDto> warehousesSource = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true).GetPagedResultDataAsync();
                warehouses = warehousesSource.ToDictionary(x => x.Id);
            }
        }

        protected override void AfterSetData()
        {
            RefreshSummaryItems();
            RaiseProperties();
        }

        protected override Task<LockResponse<ServiceProductDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockServiceProduct(id));
        }

        protected override Task<LockResponse<ServiceProductDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockServiceProduct(id));
        }

        protected override void SetCreateTitle()
        {
        }

        protected override void SetEditTitle()
        {
            Title = $"Сервисный товар №{Model.Id}";
        }

        protected override Task<Result<ServiceProductDto>> UpdateEntityAsync()
        {
            return WebClient.ExecuteApiRequestAsync(new UpdateServiceProduct(Model.Id, Mapper.Map<ServiceProductSaveDto>(Model)));
        }

        protected override bool CanEdit()
        {
            return Model.State != ServiceProductState.TransferToSupplier
                   && Model.State != ServiceProductState.OnUtilization
                   && !Model.State.Completed;
        }

        protected override void OnInitializeInDesignMode()
        {
            base.OnInitializeInDesignMode();

            warehouses = new Dictionary<int, WarehouseDto>
            {
                [75] = new WarehouseDto()
                {
                    Name = "Киев Главный"
                }
            };

            employees = new Dictionary<int, string>
            {
                [8] = "Сазонов Илья",
                [79] = "Правдивый Александр"
            };

            SummaryItems = GetSummaryItems(new ServiceProductViewItem
            {
                Id = 66600,
                PriceUsd = 999,
                LossUsd = 100.23m,
                CreatedBy = 8,
                CreatedOn = DateTime.Now.AddDays(-7),
                CompletedBy = 79,
                CompletedOn = DateTime.Now.AddDays(-2),
                State = ServiceProductState.Discounted,
                ProductDiscountId = 66700,
                BitrixId = 321123,
                WarehouseId = 75
            });
        }

        private void RefreshSummaryItems()
        {
            SummaryItems = GetSummaryItems(Model);
        }

        private void RaiseProperties()
        {
            RaisePropertiesChanged(nameof(CanProcess), nameof(CanProcessFromSupplier), nameof(IsTradeInProduct));
        }

        private Task DiscountAsync()
        {
            return ExecuteLockableOperationAsync(lockedEntity =>
            {
                ServiceProductDiscountParameter parameter = new ServiceProductDiscountParameter(lockedEntity.Id, lockedEntity.ProductId, lockedEntity.Sn, lockedEntity.WarehouseId, lockedEntity.TypeId, lockedEntity.ServiceRequestId);

                DialogDocumentManagerService.ShowView<ServiceProductDiscountViewModel>(parameter, this);
            });
        }

        private Task PutOnWarehouseAsync()
        {
            GetServiceProductWarehouseViewModel viewModel = DialogDocumentManagerService.ShowView<GetServiceProductWarehouseViewModel>(null, this);

            if (!viewModel.IsOk)
            {
                return Task.CompletedTask;
            }

            return ExecuteLockableOperationAsync(_ => WebClient.ExecuteApiRequestAsync(new PutOnWarehouseServiceProduct(Model.Id, viewModel.SelectedWarehouseId.Value)));
        }

        private Task GiveBackToSupplierAsync()
        {
            return MessageFacadeService.Confirm("Вы уверены?")
                ? ExecuteLockableOperationAsync(_ => WebClient.ExecuteApiRequestAsync(new GiveBackToSupplierServiceProduct(Model.Id)))
                : Task.CompletedTask;
        }

        private Task OnUtilizationAsync()
        {
            return ExecuteLockableOperationAsync(UtilizeInternal);

            Task UtilizeInternal(ServiceProductDto lockedEntity)
            {
                DialogDocumentManagerService.ShowView<ServiceProductUtilizeViewModel>(Model.Id, this);

                return Task.CompletedTask;
            }
        }

        private Task UtilizeAsync()
        {
            return MessageFacadeService.Confirm("Вы подтверждаете утилизацию?")
                ? ExecuteLockableOperationAsync(_ => WebClient.ExecuteApiRequestAsync(new UtilizeServiceProduct(Model.Id)))
                : Task.CompletedTask;
        }

        private Task GiveOnRepairAsync()
        {
            return MessageFacadeService.Confirm("Вы уверены?")
                ? ExecuteLockableOperationAsync(_ => WebClient.ExecuteApiRequestAsync(new GiveOnRepairServiceProduct(Model.Id)))
                : Task.CompletedTask;
        }

        private Task SupplerRemoveFromRegisterAsync()
        {
            return ExecuteLockableOperationAsync(_ => { DialogDocumentManagerService.ShowView<ServiceProductSupplierRemoveFromRegisterViewModel>(Model, this); });
        }

        private Task SupplierChangeAsync()
        {
            return ExecuteLockableOperationAsync(lockedEntity => { DialogDocumentManagerService.ShowView<ServiceProductSupplierChangeViewModel>(Model, this); });
        }

        private async Task MoveAsync()
        {
            ComboBoxItem[] warehouseItems = warehouses
                .Where(x => x.Value.Active == 1 && x.Value.TypeId == WarehouseKind.Service.Id)
                .OrderBy(x => x.Value.CityId)
                .Select(x => new ComboBoxItem(x.Value.Id, x.Value.Name))
                .ToArray();

            SelectItemViewModel viewModel = DialogDocumentManagerService.ShowView<SelectItemViewModel>(new SelectItemParameter(warehouseItems, "Выберите сервисный склад", "Склад"), this);

            if (!viewModel.IsOk)
            {
                return;
            }

            if (viewModel.SelectedItem.Value.Id == Model.WarehouseId)
            {
                MessageFacadeService.ShowNotificationError("Товар уже на выбранном складе");
                return;
            }

            await ExecuteLockableOperationAsync(_ => WebClient.ExecuteApiRequestAsync(new MoveServiceProduct(Model.Id, viewModel.SelectedItem.Value.Id)));
        }

        private Task SupplierRejectAsync()
        {
            return MessageFacadeService.Confirm("Поставщик отказал в требовании обмена/списании товара?")
                ? ExecuteLockableOperationAsync(_ => WebClient.ExecuteApiRequestAsync(new SupplierRejectServiceProduct(Model.Id)))
                : Task.CompletedTask;
        }

        private void OpenServiceRequest(int id)
        {
            Messenger.Send(new ServiceRequestViewMessage(id));
        }

        private void OpenServiceRepair(int? id)
        {
            Messenger.Send(new ServiceRepairViewMessage(id.Value));
        }

        private IEnumerable<SummaryViewItem> GetSummaryItems(ServiceProductViewItem item)
        {
            const string Format = DateFormattingRules.FullDateTimeFormat;

            yield return new SummaryViewItem("Номер", $"{item.Id}");
            yield return new SummaryViewItem("Склад", warehouses.GetValueOrDefault(item.WarehouseId)?.Name);
            yield return new SummaryViewItem("Цена", CurrencyFormatingRules.ToUsdStr(item.PriceUsd));
            yield return new SummaryViewItem("Потери", item.LossUsd.HasValue ? CurrencyFormatingRules.ToUsdStr(item.LossUsd.Value) : "?");
            yield return new SummaryViewItem("Создал", $"{employees.GetValueOrDefault(item.CreatedBy)} ({item.CreatedOn.ToString(Format)})");

            if (item.CompletedBy.HasValue && item.CompletedOn.HasValue)
            {
                yield return new SummaryViewItem("Завершил", $"{employees.GetValueOrDefault(item.CompletedBy.Value)} ({item.CompletedOn.Value.ToString(Format)})");
            }

            yield return new SummaryViewItem("Статус", $"{item.State.Name}");

            if (item.ProductDiscountId.HasValue)
            {
                yield return new SummaryViewItem("Код уценки", $"{item.ProductDiscountId}");
            }

            if (item.BitrixId.HasValue)
            {
                yield return new SummaryViewItem("Задача", $"{item.BitrixId}");
            }

            if (item.WarehouseToId.HasValue)
            {
                yield return new SummaryViewItem("Передан на", warehouses.GetValueOrDefault(item.WarehouseToId.Value)?.Name);
            }
        }

        private Task ChangeDecisionAsync()
        {
            return MessageFacadeService.Confirm("Изменить решение?")
                ? ExecuteLockableOperationAsync(_ => WebClient.ExecuteApiRequestAsync(new ChangeDecisionServiceProduct(Model.Id)))
                : Task.CompletedTask;
        }

        private bool CanChangingDecision()
        {
            return Model?.State == ServiceProductState.OnUtilization && WebClient.IsOperationAllowed(BusinessOperation.ServiceProductChangeDecision);
        }
    }
}