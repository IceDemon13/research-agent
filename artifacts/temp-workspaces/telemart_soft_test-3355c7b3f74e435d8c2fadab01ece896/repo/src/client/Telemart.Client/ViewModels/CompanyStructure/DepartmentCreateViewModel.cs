using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.CompanyStructure
{
    public sealed class DepartmentCreateViewModel : TelemartDialogViewModelBase
    {
        private readonly IMessenger _messenger;
        private readonly IErrorHandler _errorHandler;

        public DepartmentCreateViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        }

        #region INPC

        public ObservableRangeCollection<ComboBoxItem> Departments
        {
            get { return GetProperty(() => Departments); }
            set { SetProperty(() => Departments, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public string Name
        {
            get { return GetProperty(() => Name); }
            set { SetProperty(() => Name, value); }
        }

        public int? SelectedDepartmentId
        {
            get { return GetProperty(() => SelectedDepartmentId); }
            set { SetProperty(() => SelectedDepartmentId, value); }
        }

        public int? SelectedEmployeeId
        {
            get { return GetProperty(() => SelectedEmployeeId); }
            set { SetProperty(() => SelectedEmployeeId, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<DepartmentCreateViewModel> builder)
        {
            builder.Property(x => x.Name).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.SelectedEmployeeId).MatchesRule(x => x.HasValue, () => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            Title = "Создание отдела";

            await Task.WhenAll(LoadDepartmentsAsync(), LoadEmployeesAsync());
        }

        protected override async Task HandleOkAsync()
        {
            DepartmentCreateDto createDto = new DepartmentCreateDto
            {
                Name = Name,
                DepartmentParentId = SelectedDepartmentId,
                EmployeeId = SelectedEmployeeId
            };

            Result<DepartmentDto> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateDepartment(createDto)),
                "создании отдела",
                "Отдел создан",
                this,
                true);

            if (result?.IsSuccess == true)
            {
                _messenger.Send(new DepartmentMessage(result.Data, MessageType.Added));

                CloseOk();
            }
        }

        private async Task LoadDepartmentsAsync()
        {
            List<DepartmentDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            Departments = dtos.Select(x => new ComboBoxItem(x.Id, x.Name)).ToObservableRangeCollection();
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees.Select(x => new ComboBoxItem(x.Id, x.Name)).OrderBy(x => x.DisplayValue).ToReadOnlyObservableCollection();
        }
    }
}