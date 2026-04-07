using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.PackList;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.PackList;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public sealed class PackListTransferPackViewModel : TelemartDialogViewModelBase
    {
        private PackListDto _packList;
        private WarehouseAllowedEmployeesDto _warehouseAllowedEmployees;
        private WarehouseDto _warehouse;

        public PackListTransferPackViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public int? SelectedCollectorEmployeeId
        {
            get { return GetProperty(() => SelectedCollectorEmployeeId); }
            set { SetProperty(() => SelectedCollectorEmployeeId, value); }
        }

        public bool IsAllowChangeCollector => WebClient.IsOperationAllowed(BusinessOperation.PackListTransferChangeCollectorAllow);

        public int GetCollector() => SelectedCollectorEmployeeId ?? 0;

        public static void BuildMetadata(MetadataBuilder<PackListTransferPackViewModel> builder)
        {
            builder.Property(x => x.SelectedCollectorEmployeeId).MatchesRule(x => x.HasValue, () => "Выберите сборщика");
        }

        protected override async Task HandleLoadedAsync()
        {
            PackListTransferPackParameter parameter = (PackListTransferPackParameter)Parameter;

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

            Title = $"Изменение сборщика";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees.OrderBy(x => x.Name).ToReadOnlyObservableCollection();
        }

        private async Task LoadWarehousesAllowedEmployeesAsync(int warehouseId)
        {
            List<WarehouseAllowedEmployeesDto> warehousesAllowedEmployees = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseAllowedEmployees());

            _warehouseAllowedEmployees = warehousesAllowedEmployees?.FirstOrDefault(x => x.WarehouseId == warehouseId);
        }

        private async Task LoadPackListAsync(int id)
        {
            _packList = await WebClient.ExecuteApiRequestAsync(new QueryPackList(id));

            SelectedCollectorEmployeeId = _packList.CollectorEmployeeId;
        }

        private async Task LoadWarehouseAsync(int warehouseId)
        {
            _warehouse = await WebClient.ExecuteApiRequestAsync(new QueryWarehouse(warehouseId));
        }
    }
}