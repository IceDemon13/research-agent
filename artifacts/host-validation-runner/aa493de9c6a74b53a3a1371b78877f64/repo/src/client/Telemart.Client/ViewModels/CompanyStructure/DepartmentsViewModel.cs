using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
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
    public sealed class DepartmentsViewModel : TelemartDialogViewModelBase
    {
        private readonly IMessenger _messenger;
        private readonly IErrorHandler _errorHandler;
        private readonly IMapper _mapper;
        private ReadOnlyObservableCollection<DepartmentDto> _departments;

        public DepartmentsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger,
            IErrorHandler errorHandler,
            IMapper mapper)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            AddDepartmentCommand = new DelegateCommand(AddDepartment, () => WebClient.IsOperationAllowed(BusinessOperation.CompanyStructureCreateDepartment));
            EditDepartmentCommand = new DelegateCommand<DepartmentViewItem>(EditDepartment, x => x != null);
            RemoveDepartmentsCommand = new AsyncCommand<DepartmentViewItem>(RemoveDepartmentAsync, x => x != null && WebClient.IsOperationAllowed(BusinessOperation.CompanyStructureRemoveDepartment));

            _messenger.Register<DepartmentMessage>(this, OnDepartmentMessage);
        }

        #region INPC

        public ObservableRangeCollection<DepartmentViewItem> Departments
        {
            get { return GetProperty(() => Departments); }
            set { SetProperty(() => Departments, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public DepartmentViewItem SelectedDepartment
        {
            get { return GetProperty(() => SelectedDepartment); }
            set { SetProperty(() => SelectedDepartment, value); }
        }

        #endregion

        #region Commands

        public IDelegateCommand AddDepartmentCommand { get; }

        public IDelegateCommand EditDepartmentCommand { get; }

        public IAsyncCommand RemoveDepartmentsCommand { get; }

        #endregion

        protected override Task HandleLoadedAsync()
        {
            Title = "Отделы";

            return RefreshAsync();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private async Task RefreshAsync()
        {
            await Task.WhenAll(LoadDepartmentsAsync(), LoadEmployeesAsync());
        }

        private async Task LoadDepartmentsAsync()
        {
            List<DepartmentDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            _departments = dtos.ToReadOnlyObservableCollection();

            Departments = _departments.Where(x => x.Active).Select(x => _mapper.Map<DepartmentViewItem>(x)).ToObservableRangeCollection();
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees.Select(x => new ComboBoxItem(x.Id, x.Name)).OrderBy(x => x.DisplayValue).ToReadOnlyObservableCollection();
        }

        private void AddDepartment()
        {
            DialogDocumentManagerService.ShowView<DepartmentCreateViewModel>(null, this);
        }

        private void EditDepartment(DepartmentViewItem item)
        {
            DialogDocumentManagerService.ShowView<DepartmentEditViewModel>(new DepartmentViewParameter(item.Id), this);
        }

        private async Task RemoveDepartmentAsync(DepartmentViewItem item)
        {
            if (MessageFacadeService.Confirm("Вы уверены?", "Удаление отдела из структуры компании."))
            {
                Result result = await _errorHandler.HandleErrorsAsync(
                    _ => WebClient.ExecuteApiRequestAsync(new DisableDepartment(item.Id)),
                    "удалении отдела",
                    null,
                    this,
                    true,
                    showNotification: false);

                if (result.IsSuccess)
                {
                    Departments.Remove(item);
                }
            }
        }

        private void OnDepartmentMessage(DepartmentMessage message)
        {
            switch (message.MessageType)
            {
                case MessageType.Added:
                    Departments.Insert(0, _mapper.Map<DepartmentViewItem>(message.Entity));
                    break;
                case MessageType.Changed:
                    Departments.DoActionWithItem(x => x.Id == message.Entity.Id, viewItem => _mapper.Map(message.Entity, viewItem));
                    break;
            }
        }
    }
}