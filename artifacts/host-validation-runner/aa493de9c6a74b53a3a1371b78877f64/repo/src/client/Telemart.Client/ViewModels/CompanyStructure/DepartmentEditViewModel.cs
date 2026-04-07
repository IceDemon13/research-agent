using System;
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
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.CompanyStructure
{
    public sealed class DepartmentEditViewModel : TelemartEditorViewModelBase<DepartmentDto, DepartmentViewParameter, DepartmentViewItem>
    {
        private List<EmployeeDto> _allEmployees;
        private List<DepartmentDto> _allDepartments;

        public DepartmentEditViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            EditCommand = new DelegateCommand(() => IsEdit = true, () => WebClient.IsOperationAllowed(BusinessOperation.CompanyStructureEditDepartment) && !IsEdit);
        }

        public IDelegateCommand EditCommand { get; }

        #region INPC

        public ReadOnlyObservableCollection<ComboBoxItem> Departments
        {
            get { return GetProperty(() => Departments); }
            set { SetProperty(() => Departments, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<EmployeeDepartmentViewItem> DepartmentEmployees
        {
            get { return GetProperty(() => DepartmentEmployees); }
            set { SetProperty(() => DepartmentEmployees, value); }
        }

        public ReadOnlyObservableCollection<DepartmentEmployeeType> DepartmentEmployeeTypes
        {
            get { return GetProperty(() => DepartmentEmployeeTypes); }
            set { SetProperty(() => DepartmentEmployeeTypes, value); }
        }

        public EmployeeDepartmentViewItem SelectedDepartmentEmployee
        {
            get { return GetProperty(() => SelectedDepartmentEmployee); }
            set { SetProperty(() => SelectedDepartmentEmployee, value); }
        }

        public bool IsEdit
        {
            get { return GetProperty(() => IsEdit); }
            set { SetProperty(() => IsEdit, value); }
        }

        #endregion

        #region DialogSettings

        public override int Height => 660;

        public override int MinHeight => 660;

        public override int MinWidth => 1084;

        public override int Width => 1084;

        #endregion

        protected override string CreatedActionMessage => string.Empty;

        protected override string EntityName => "Отдел";

        protected override string UpdatedActionMessage => "сохранен";

        protected override Task<DepartmentDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteApiRequestAsync(new QueryDepartment(id));
        }

        protected override async Task HandleLoadedAsync()
        {
            DepartmentEmployeeTypes = Dictionaries.GetItems<DepartmentEmployeeType>().ToReadOnlyObservableCollection();

            await Task.WhenAll(LoadDepartmentsAsync(), LoadEmployeesAsync());

            await base.HandleLoadedAsync();
        }

        protected override void AfterSetData()
        {
            DepartmentEmployees = _allEmployees
                .Where(x => Model.Employees.Contains(x.Id) && x.Active && x.FiredOn == null && x.FiredBy == null)
                .Select(x => new EmployeeDepartmentViewItem(x.Id, x.Name, x.Position, x.Phone1, MapToDepartmentEmployeeType(x)))
                .OrderBy(x => x.DepartmentEmployeeType.Id)
                .ToReadOnlyObservableCollection();
        }

        protected override Task<LockResponse<DepartmentDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<DepartmentDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
        }

        protected override void SetEditTitle()
        {
            Title = $"Отдел {Model?.Name} (№ {Model?.Id})";
        }

        protected override Task<Result<DepartmentDto>> CreateEntityAsync()
        {
            throw new NotSupportedException();
        }

        protected override Task<Result<DepartmentDto>> UpdateEntityAsync()
        {
            DepartmentUpdateDto updateDto = new DepartmentUpdateDto
            {
                Id = Model.Id,
                Name = Model.Name,
                DepartmentParentId = Model.ParentDepartmentId,
                EmployeeId = Model.EmployeeId
            };

            return WebClient.ExecuteApiRequestAsync(new UpdateDepartment(updateDto));
        }

        protected override object CreateEntityMessage(DepartmentDto dto, MessageType messageType)
        {
            return new DepartmentMessage(dto, messageType);
        }

        private async Task LoadDepartmentsAsync()
        {
            List<DepartmentDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            _allDepartments = dtos;

            Departments = dtos.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees()).GetPagedResultDataAsync();

            _allEmployees = employees;

            Employees = employees.Where(x => x.Active && x.FiredOn == null && x.FiredBy == null)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();
        }

        private DepartmentEmployeeType MapToDepartmentEmployeeType(EmployeeDto employeeDto)
        {
            if (employeeDto != null && _allDepartments?.Any(x => x.EmployeeId == employeeDto.Id) == true)
            {
                return DepartmentEmployeeType.Manager;
            }

            return DepartmentEmployeeType.Worker;
        }
    }
}