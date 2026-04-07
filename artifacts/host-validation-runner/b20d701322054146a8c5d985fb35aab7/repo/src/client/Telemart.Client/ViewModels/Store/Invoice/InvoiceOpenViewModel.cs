using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Invoice
{
    public sealed class InvoiceOpenViewModel : TelemartDialogViewModelBase
    {
        public InvoiceOpenViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Title = "Открытие накладной";
        }

        public InvoiceOpenViewModel()
        {
        }

        #region INPC

        public DateTime? DateClose
        {
            get { return GetProperty(() => DateClose); }
            set { SetProperty(() => DateClose, value); }
        }

        public DateTime DateMax
        {
            get { return GetProperty(() => DateMax); }
            private set { SetProperty(() => DateMax, value); }
        }

        public DateTime DateMin
        {
            get { return GetProperty(() => DateMin); }
            private set { SetProperty(() => DateMin, value); }
        }

        public int? EmployeeRequestId
        {
            get { return GetProperty(() => EmployeeRequestId); }
            set { SetProperty(() => EmployeeRequestId, value); }
        }

        public ObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            private set { SetProperty(() => Employees, value); }
        }

        #endregion

        public static void BuildMetadata(MetadataBuilder<InvoiceOpenViewModel> builder)
        {
            builder.Property(x => x.DateClose).Required(() => Resources.RequiredErrorMessage);
            builder.Property(x => x.EmployeeRequestId).Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            List<EmployeeDto> employeeList = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            IEnumerable<ComboBoxItem> employees = employeeList
                .Where(x => x.Active && x.HasAnyRole(Role.Admin, Role.Product, Role.Logist))
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue);

            Employees = new ObservableCollection<ComboBoxItem>(employees);

            DateMin = DateTime.Today;
            DateMax = DateTime.Today.AddDays(1);

            DateClose = DateTime.Now.AddMinutes(10);
        }

        protected override Task HandleOkAsync()
        {
            IsOk = DateClose.HasValue && EmployeeRequestId.HasValue;

            if (IsOk)
            {
                Close();
            }

            return Task.CompletedTask;
        }
    }
}
