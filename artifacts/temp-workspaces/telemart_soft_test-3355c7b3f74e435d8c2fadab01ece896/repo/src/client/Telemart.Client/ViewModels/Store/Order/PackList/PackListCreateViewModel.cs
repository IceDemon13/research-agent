using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Microsoft.Extensions.Logging;
using Telemart.Client.Common.Services;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.PackList;
using Telemart.Client.Data.Requests.Features.Warehouse;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.PackList;
using Telemart.Client.TransferObjects.Warehouse;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Common;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public sealed class PackListCreateViewModel : TelemartDialogViewModelBase
    {
        private ReadOnlyObservableCollection<EmployeeDto> _allEmployees;
        private ReadOnlyObservableCollection<WarehouseAllowedEmployeesDto> _allWarehousesAllowedEmployees;

        public PackListCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public PackListCreateViewModel()
        {
        }

        public ReadOnlyObservableCollection<CarryType> CarryTypes
        {
            get { return GetProperty(() => CarryTypes); }
            private set { SetProperty(() => CarryTypes, value); }
        }

        public ReadOnlyObservableCollection<Subdivision> Subdivisions
        {
            get { return GetProperty(() => Subdivisions); }
            private set { SetProperty(() => Subdivisions, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDto> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public PackListCreateViewItem Model
        {
            get { return GetProperty(() => Model); }
            private set { SetProperty(() => Model, value); }
        }

        public PackListDto NewPackList { get; private set; }

        public DateTime NewParkListDate()
        {
            return Model?.Date ?? DateTime.Today;
        }

        public bool ChangePackListDate => !WebClient.IsOperationAllowed(BusinessOperation.PackListDateChanging);

        protected override async Task HandleLoadedAsync()
        {
            PackListCreateParameter p = (PackListCreateParameter)Parameter;

            await Task.WhenAll(LoadEmployeesAsync(), LoadWarehousesAllowedEmployeesAsync());

            Subdivisions = Dictionaries.GetItems<Subdivision>()
                .Where(x => WebClient.AuthenticatedEmployee.AllowSubdivisions.Contains(x.Id))
                .ToReadOnlyObservableCollection();

            CarryTypes = Dictionaries.GetItems<CarryType>()
                .Where(x => p.WarehouseDeliveryCarryIds.Contains(x.Id) && (x.IsActive() || p.AvailableCarryIds.Contains(x.Id)))
                .ToReadOnlyObservableCollection();

            Model = new PackListCreateViewItem
            {
                State = p.State,
                Warehouse = p.Warehouse,
                Time = p.Time,
                Date = p.Date
            };

            Model.PackagerEmployeeId = WebClient.AuthenticatedEmployee.Id;

            WarehouseAllowedEmployeesDto warehouseAllowedEmployees = _allWarehousesAllowedEmployees?.FirstOrDefault(x => x.WarehouseId == p.Warehouse.Id);

            Employees = _allEmployees?.Where(x => warehouseAllowedEmployees?.EmployeeIds?.Contains(x.Id) == true && (x.Roles.Contains(Role.Packager.Name) || x.Roles.Contains(Role.Warehouse.Name)))
                .OrderBy(x => x.Name)
                .ToReadOnlyObservableCollection();

            Title = "Создание листа на сборку";
        }

        protected override async Task HandleOkAsync()
        {
            if (DateTime.Now.Date != Model.Date)
            {
                DelayedConfirmViewModel viewModel = DialogDocumentManagerService.ShowView<DelayedConfirmViewModel>(
                    "Дата отличается от текущей. Создать лист на сборку?",
                    this);

                if (!viewModel.IsOk)
                {
                    return;
                }
            }

            try
            {
                PackListCreateDto createDto = new PackListCreateDto
                {
                    SubdivisionId = Model.SubdivisionId.Value,
                    WarehouseId = Model.Warehouse.Id,
                    CarryIds = Model.CarryIds.ToArray(),
                    Time = Model.Date.Add(Model.Time.Value.TimeOfDay),
                    Date = Model.Date,
                    PackagerEmployeeId = Model.PackagerEmployeeId.Value,
                    CollectorEmployeeId = Model.CollectorEmployeeId.Value
                };

                Result<PackListDto> result = await WebClient.ExecuteApiRequestAsync(new CreatePackList(createDto));

                NewPackList = result.Data;

                if (result.Warnings.Any())
                {
                    MessageFacadeService.ShowNotificationWarning("Лист на сборку создан с предупреждениями");
                    ShowValidationResultView("Предупрежедения", result.Warnings.Select(x => new ValidationResultItem(x, false)).ToList());
                }
                else
                {
                    MessageFacadeService.ShowNotificationInfo("Лист на сборку успешно создан");
                }

                IsOk = true;

                Close();
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании листа на сборку");
                ShowValidationResultView("Ошибки при создании листа на сборку", exception.GetErrorItems());
            }
            catch (UnexpectedErrorException exception)
            {
                Logger.LogError(exception, "Failed to create pack list");
                ShowValidationResultView(Resources.ServerConnectError, new[] { new ValidationResultItem(Resources.ServerUnavailable, true) });
            }
            catch (Exception exception)
            {
                MessageFacadeService.ShowNotificationError("Ошибка при создании листа на сборку");
                Logger.LogError(exception, "Error while creating pack list");
            }
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            _allEmployees = employees.ToReadOnlyObservableCollection();
        }

        private async Task LoadWarehousesAllowedEmployeesAsync()
        {
            List<WarehouseAllowedEmployeesDto> warehousesAllowedEmployees = await WebClient.ExecuteApiRequestAsync(new QueryWarehouseAllowedEmployees());

            _allWarehousesAllowedEmployees = warehousesAllowedEmployees.ToReadOnlyObservableCollection();
        }
    }
}