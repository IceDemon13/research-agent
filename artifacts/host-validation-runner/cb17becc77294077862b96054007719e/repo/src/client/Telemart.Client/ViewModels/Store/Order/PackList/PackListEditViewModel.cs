using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.PackList;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.PackList;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public sealed class PackListEditViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private PackListDto _packList;
        private WarehouseDto _warehouse;
        private WarehouseAllowedEmployeesDto _warehouseAllowedEmployees;

        public PackListEditViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public int? CurrentPackagerEmployeeId
        {
            get { return GetProperty(() => CurrentPackagerEmployeeId); }
            set { SetProperty(() => CurrentPackagerEmployeeId, value); }
        }

        public int? CurrentCollectorEmployeeId
        {
            get { return GetProperty(() => CurrentCollectorEmployeeId); }
            set { SetProperty(() => CurrentCollectorEmployeeId, value); }
        }

        public int? SelectedPackagerEmployeeId
        {
            get { return GetProperty(() => SelectedPackagerEmployeeId); }
            set { SetProperty(() => SelectedPackagerEmployeeId, value); }
        }

        public int? SelectedCollectorEmployeeId
        {
            get { return GetProperty(() => SelectedCollectorEmployeeId); }
            set { SetProperty(() => SelectedCollectorEmployeeId, value); }
        }

        public bool AllowChangePackerAndCollector
        {
            get { return GetProperty(() => AllowChangePackerAndCollector); }
            set { SetProperty(() => AllowChangePackerAndCollector, value); }
        }

        public static void BuildMetadata(MetadataBuilder<PackListEditViewModel> builder)
        {
            builder.Property(x => x.SelectedPackagerEmployeeId).MatchesRule(x => x.HasValue, () => "Выберите упаковщика");
            builder.Property(x => x.SelectedCollectorEmployeeId).MatchesRule(x => x.HasValue, () => "Выберите сборщика");
        }

        protected override async Task HandleLoadedAsync()
        {
            PackListEditParameter parameter = (PackListEditParameter)Parameter;

            await Task.WhenAll(
                LoadEmployeesAsync(),
                LoadPackListAsync(parameter.PackListId),
                LoadWarehousesAllowedEmployeesAsync(parameter.WarehouseId),
                LoadWarehouseAsync(parameter.WarehouseId));

            Employees = Employees
                .Where(x => _warehouseAllowedEmployees?.EmployeeIds.Contains(x.Id) == true && (x.Roles.Contains(Role.Packager.Name) || x.Roles.Contains(Role.Warehouse.Name)))
                .ToReadOnlyObservableCollection();

            if (Employees?.Any() != true)
            {
                MessageFacadeService.ShowMessageBoxWarning($"К складу {_warehouse?.Name} нет прикрепленных сотрудников");
            }

            Title = $"Изменение Листа на сборку №{parameter.PackListId}";
        }

        protected override async Task HandleOkAsync()
        {
            if (CurrentCollectorEmployeeId == SelectedCollectorEmployeeId && CurrentPackagerEmployeeId == SelectedPackagerEmployeeId)
            {
                MessageFacadeService.ShowNotificationInfo("Нет изменений для сохранения");
                return;
            }

            string partInfo = (CurrentCollectorEmployeeId != SelectedCollectorEmployeeId) && (CurrentPackagerEmployeeId != SelectedPackagerEmployeeId)
                ? "упаковщика и сборщика"
                : CurrentCollectorEmployeeId != SelectedCollectorEmployeeId
                    ? "сборщика"
                    : "упаковщика";

            if (!MessageFacadeService.Confirm($"Вы уверены, что хотитите изменить {partInfo} в листе на сборку"))
            {
                Close();
                return;
            }

            Result<PackListDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new EditPackList(_packList.Id, new PackListEditDto(SelectedCollectorEmployeeId.Value, SelectedPackagerEmployeeId.Value, _warehouse?.Id ?? 0))),
                "изменении Листа на сборку",
                "Листа на сборку изменен",
                this,
                true);

            if (result.IsSuccess)
            {
                CloseOk();
            }
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees.OrderBy(x => x.Name).ToReadOnlyObservableCollection();
        }

        private async Task LoadPackListAsync(int id)
        {
            _packList = await WebClient.ExecuteApiRequestAsync(new QueryPackList(id));

            CurrentCollectorEmployeeId = SelectedCollectorEmployeeId = _packList.CollectorEmployeeId;

            CurrentPackagerEmployeeId = SelectedPackagerEmployeeId = _packList.PackagerEmployeeId;

            AllowChangePackerAndCollector = _packList.CompletedBy == null;
        }

        private async Task LoadWarehousesAllowedEmployeesAsync(int warehouseId)
        {
            List<WarehouseAllowedEmployeesDto> warehousesAllowedEmployees = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseAllowedEmployees());

            _warehouseAllowedEmployees = warehousesAllowedEmployees?.FirstOrDefault(x => x.WarehouseId == warehouseId);
        }

        private async Task LoadWarehouseAsync(int warehouseId)
        {
            _warehouse = await WebClient.ExecuteApiRequestAsync(new QueryWarehouse(warehouseId));
        }
    }
}