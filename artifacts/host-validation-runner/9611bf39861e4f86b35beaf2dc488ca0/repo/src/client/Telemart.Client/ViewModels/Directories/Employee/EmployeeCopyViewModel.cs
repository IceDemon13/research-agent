using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Directories.Employee
{
    public sealed class EmployeeCopyViewModel : TelemartDialogViewModelBase
    {
        public EmployeeCopyViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            ErrorHandler = errorHandler;
        }

        #region INPC

        public int? SelectedEmployeeId
        {
            get { return GetProperty(() => SelectedEmployeeId); }
            set { SetProperty(() => SelectedEmployeeId, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> CopyChoices
        {
            get { return GetProperty(() => CopyChoices); }
            set { SetProperty(() => CopyChoices, value); }
        }

        public bool IsCopyRole
        {
            get { return GetProperty(() => IsCopyRole); }
            set { SetProperty(() => IsCopyRole, value); }
        }

        public int? SelectedRoleCopyChoice
        {
            get { return GetProperty(() => SelectedRoleCopyChoice); }
            set { SetProperty(() => SelectedRoleCopyChoice, value); }
        }

        public bool IsCopyOperation
        {
            get { return GetProperty(() => IsCopyOperation); }
            set { SetProperty(() => IsCopyOperation, value); }
        }

        public int? SelectedOparationCopyChoice
        {
            get { return GetProperty(() => SelectedOparationCopyChoice); }
            set { SetProperty(() => SelectedOparationCopyChoice, value); }
        }

        public bool IsCopySubdivision
        {
            get { return GetProperty(() => IsCopySubdivision); }
            set { SetProperty(() => IsCopySubdivision, value); }
        }

        public int? SelectedSubdivisionCopyChoice
        {
            get { return GetProperty(() => SelectedSubdivisionCopyChoice); }
            set { SetProperty(() => SelectedSubdivisionCopyChoice, value); }
        }

        public bool IsCopyWarehouses
        {
            get { return GetProperty(() => IsCopyWarehouses); }
            set { SetProperty(() => IsCopyWarehouses, value); }
        }

        public int? SelectedWarehousesCopyChoice
        {
            get { return GetProperty(() => SelectedWarehousesCopyChoice); }
            set { SetProperty(() => SelectedWarehousesCopyChoice, value); }
        }

        public bool IsCopyCashbox
        {
            get { return GetProperty(() => IsCopyCashbox); }
            set { SetProperty(() => IsCopyCashbox, value); }
        }

        public int? SelectedCashboxCopyChoice
        {
            get { return GetProperty(() => SelectedCashboxCopyChoice); }
            set { SetProperty(() => SelectedCashboxCopyChoice, value); }
        }

        public bool IsCopyCategory
        {
            get { return GetProperty(() => IsCopyCategory); }
            set { SetProperty(() => IsCopyCategory, value); }
        }

        public int? SelectedCategoryCopyChoice
        {
            get { return GetProperty(() => SelectedCategoryCopyChoice); }
            set { SetProperty(() => SelectedCategoryCopyChoice, value); }
        }

        #endregion

        private IErrorHandler ErrorHandler { get; }

        protected override async Task HandleLoadedAsync()
        {
            CopyChoices = Dictionaries.GetItems<CopyActionType>()
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .ToReadOnlyObservableCollection();

            EmployeeCopyParameter copyParameter = (EmployeeCopyParameter)Parameter;

            await RefreshEmployeesAsync(copyParameter.EmployeeId);

            IsCopyRole = true;
            IsCopyCashbox = true;
            IsCopyCategory = true;
            IsCopyOperation = true;
            IsCopySubdivision = true;
            IsCopyWarehouses = true;

            SelectedRoleCopyChoice = CopyActionType.Override.Id;
            SelectedCashboxCopyChoice = CopyActionType.Override.Id;
            SelectedCategoryCopyChoice = CopyActionType.Override.Id;
            SelectedOparationCopyChoice = CopyActionType.Override.Id;
            SelectedSubdivisionCopyChoice = CopyActionType.Override.Id;
            SelectedWarehousesCopyChoice = CopyActionType.Override.Id;

            Title = "Копирование настроек сотрудника";
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        protected override bool CanOk()
        {
            return SelectedEmployeeId != null
                   && (IsCopyRole
                       || IsCopyCashbox
                       || IsCopyCategory
                       || IsCopyOperation
                       || IsCopySubdivision
                       || IsCopyWarehouses)
                   && SelectedCashboxCopyChoice.HasValue
                   && SelectedCategoryCopyChoice.HasValue
                   && SelectedOparationCopyChoice.HasValue
                   && SelectedRoleCopyChoice.HasValue
                   && SelectedSubdivisionCopyChoice.HasValue
                   && SelectedWarehousesCopyChoice.HasValue;
        }

        private async Task RefreshEmployeesAsync(int employeeId)
        {
            List<EmployeeDto> employeesDto = await ErrorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new QueryEmployees()).GetPagedResultDataAsync(),
                "получении сотрудников",
                null,
                this,
                true,
                showNotification: false);

            if (employeesDto != null)
            {
                Employees = employeesDto
                    .Where(x => x.Id != employeeId && x.Active)
                    .OrderBy(x => x.Name)
                    .Select(y => new ComboBoxItem(y.Id, y.Name))
                    .ToReadOnlyObservableCollection();
            }
        }
    }
}