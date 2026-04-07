using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Cashbox;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.LegalEntity;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Dictionaries.Constants;
using Telemart.Client.Extensions;
using Telemart.Client.FiscalRegistrar;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Cashbox;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Cashbox
{
    public sealed class CashboxViewModel : TelemartEditorViewModelBase<CashboxDto, CashboxEditParameter, CashboxViewItem>
    {
        private bool canEdit;

        public CashboxViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
        }

        public CashboxViewModel()
        {
        }

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<CashboxType> Types
        {
            get { return GetProperty(() => Types); }
            private set { SetProperty(() => Types, value); }
        }

        public ReadOnlyObservableCollection<FiscalRegistrarType> ConnectTypes
        {
            get { return GetProperty(() => ConnectTypes); }
            private set { SetProperty(() => ConnectTypes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> CashboxCredentialLogins
        {
            get { return GetProperty(() => CashboxCredentialLogins); }
            private set { SetProperty(() => CashboxCredentialLogins, value); }
        }

        public ReadOnlyObservableCollection<Currency> Currencies
        {
            get { return GetProperty(() => Currencies); }
            private set { SetProperty(() => Currencies, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Payments
        {
            get { return GetProperty(() => Payments); }
            private set { SetProperty(() => Payments, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> LegalEntities
        {
            get { return GetProperty(() => LegalEntities); }
            private set { SetProperty(() => LegalEntities, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> StrongboxCashboxes
        {
            get { return GetProperty(() => StrongboxCashboxes); }
            private set { SetProperty(() => StrongboxCashboxes, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Warehouses
        {
            get { return GetProperty(() => Warehouses); }
            private set { SetProperty(() => Warehouses, value); }
        }

        public bool AllowEditLegalEntity
        {
            get { return GetProperty(() => AllowEditLegalEntity); }
            private set { SetProperty(() => AllowEditLegalEntity, value); }
        }

        #endregion

        protected override string CreatedActionMessage => "создана";

        protected override string EntityName => "Касса";

        protected override string UpdatedActionMessage => "сохранена";

        protected override object CreateEntityMessage(CashboxDto dto, MessageType messageType)
        {
            return new CashboxMessage(dto, messageType);
        }

        protected override Task<CashboxDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryCashbox(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            canEdit = WebClient.IsOperationAllowed(BusinessOperation.CashboxUpdate);

            Types = Dictionaries.GetItems<CashboxType>().ToReadOnlyObservableCollection();
            Payments = GetPayments().ToReadOnlyObservableCollection();
            Currencies = Dictionaries.GetCurrencies().ToReadOnlyObservableCollection();
            ConnectTypes = Dictionaries.GetItems<FiscalRegistrarType>().ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            List<CashboxCredentialDto> cashboxCredentials = await WebClient.ExecuteApiRequestAsync(new QueryCashboxCredentials(Model.Id));

            CashboxCredentialLogins = cashboxCredentials
                .Where(x => x.ProviderId == CashboxProvider.CheckboxId && x.CashboxId == Model.Id)
                .Select(x => new ComboBoxItem(x.Id, x.Login))
                .ToReadOnlyObservableCollection();

            AllowEditLegalEntity = Model.LegalEntityId == null;

            await Task.WhenAll(RefreshEmployeesAsync(), RefreshLegalEntitiesAsync(), RefreshStrongboxCashboxesAsync(), RefreshWarehousesAsync());

            SetEmployees();
        }

        protected override void AfterSetData()
        {
            SetEmployees();
        }

        protected override Task<LockResponse<CashboxDto>> LockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new LockCashbox(id));
        }

        protected override Task<LockResponse<CashboxDto>> UnlockEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new UnlockCashbox(id));
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание кассы";
        }

        protected override void SetEditTitle()
        {
            Title = $"Касса \"{Model.Name}\" ({Model.Id})";
        }

        protected override Task<Result<CashboxDto>> UpdateEntityAsync()
        {
            CashboxSaveDto saveDto = Mapper.Map<CashboxSaveDto>(Model);
            UpdateCashbox gatewayRequest = new UpdateCashbox(Model.Id, saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override Task<Result<CashboxDto>> CreateEntityAsync()
        {
            CashboxSaveDto saveDto = Mapper.Map<CashboxSaveDto>(Model);
            CreateCashbox gatewayRequest = new CreateCashbox(saveDto);
            return WebClient.ExecuteApiRequestAsync(gatewayRequest);
        }

        protected override bool CanEdit()
        {
            return canEdit;
        }

        protected override void OnInitializeInDesignModeInternal()
        {
            Types = new[] { CashboxType.Retail }.ToReadOnlyObservableCollection();
            Payments = new[]
            {
                new ComboBoxItem(CashboxConstants.NotSetCashboxPaymentId, "Не задан"),
                new ComboBoxItem(Payment.CashId, "Наличные"),
                new ComboBoxItem(Payment.BankId, "Банк"),
                new ComboBoxItem(Payment.WmuId, "WMU"),
                new ComboBoxItem(Payment.NoId, "Нет"),
                new ComboBoxItem(Payment.BitcoinId, "Bitcoin")
            }.ToReadOnlyObservableCollection();
            Currencies = new[] { Currency.Uah }.ToReadOnlyObservableCollection();
            Employees = new[] { new ComboBoxItem(79, "Demo") }.ToReadOnlyObservableCollection();

            Model.Id = 23;
            Model.Name = "Test";
            Model.TypeId = CashboxType.Retail.Id;
            Model.EmployeeId = 79;
            Model.Employee = new ComboBoxItem(79, "Demo");
            Model.CurrencyId = Currency.Uah.Id;
            Model.AllowedPayments = new ObservableCollection<int>
            {
                Payment.CashId,
                Payment.BankId,
                Payment.WmuId,
                Payment.NoId,
                Payment.BitcoinId,
                CashboxConstants.NotSetCashboxPaymentId
            };
            Model.AutoPay = true;
            Model.IsActive = true;
            Model.EmployeeLockId = null;
            Model.EmployeeLockName = null;
        }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true)
                .GetPagedResultDataAsync();

            Employees = employees
                .Where(x => x.Active || x.Id == Model.EmployeeId)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name, x.Active))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshLegalEntitiesAsync()
        {
            List<LegalEntityDto> legalEntities = await WebClient.ExecuteApiRequestAsync(new QueryLegalEntities(), true);

            LegalEntities = legalEntities
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshStrongboxCashboxesAsync()
        {
            List<CashboxDto> cashboxes = await WebClient.ExecuteApiRequestAsync(new QueryCashboxes(), true);

            StrongboxCashboxes = cashboxes
                .Where(x => x.TypeId == CashboxType.Strongbox.Id)
                .OrderBy(x => x.Name)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshWarehousesAsync()
        {
            PagedResult<WarehouseDto> warehouses = await WebClient.ExecuteApiRequestAsync(new QueryWarehouses(), true);

            List<WarehouseDto> pickupWarehouses = warehouses.Data.Where(x => x.TypeId == WarehouseKind.Pickup.Id && x.Active == 1).ToList();

            Warehouses = pickupWarehouses.Select(y => new ComboBoxItem(y.Id, y.Name)).OrderBy(x => x.DisplayValue).ToReadOnlyObservableCollection();
        }

        private void SetEmployees()
        {
            if (Employees != null && Model.EmployeeId.HasValue)
            {
                ModelOriginal.Employee = Model.Employee = Employees.FirstOrDefault(x => x.Id == Model.EmployeeId);
            }
        }

        private IEnumerable<ComboBoxItem> GetPayments()
        {
            yield return new ComboBoxItem(CashboxConstants.NotSetCashboxPaymentId, "Не задан");

            foreach (Payment payment in Dictionaries.GetItems<Payment>().Where(x => x.Active))
            {
                yield return new ComboBoxItem(payment.Id, payment.Name);
            }
        }
    }
}