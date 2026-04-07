using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Backlog;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Quotas;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Quotas;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Backlog
{
    public sealed class BacklogTaskAddToPlanViewModel : TelemartDialogViewModelBase
    {
        private const string BlackColor = "#000000";
        private const string RedColor = "#FF0000";
        private BacklogTaskAddToPlanParameter _parameter;
        private IReadOnlyDictionary<int, int> _employeeDepartments;
        private IReadOnlyCollection<BacklogTaskDto> _backlogTasks;
        private IReadOnlyCollection<QuotaDto> _quotas;
        private int _maxQuotaIt;

        public BacklogTaskAddToPlanViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService)
            : base(webClient, dictionaries, messageFacadeService)
        {
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Quotas
        {
            get { return GetProperty(() => Quotas); }
            private set { SetProperty(() => Quotas, value); }
        }

        public int? SelectedQuotaId
        {
            get { return GetProperty(() => SelectedQuotaId); }
            set { SetProperty(() => SelectedQuotaId, value, CalculatePlans); }
        }

        public int? Estimate
        {
            get { return GetProperty(() => Estimate); }
            set { SetProperty(() => Estimate, value, CalculatePlans); }
        }

        public string DepartmentQuotaPlanString
        {
            get { return GetProperty(() => DepartmentQuotaPlanString); }
            private set { SetProperty(() => DepartmentQuotaPlanString, value); }
        }

        public string TotalQuotaPlanString
        {
            get { return GetProperty(() => TotalQuotaPlanString); }
            private set { SetProperty(() => TotalQuotaPlanString, value); }
        }

        public string TotalQuotaPlanColor
        {
            get { return GetProperty(() => TotalQuotaPlanColor); }
            private set { SetProperty(() => TotalQuotaPlanColor, value); }
        }

        public string DepartmentQuotaPlanColor
        {
            get { return GetProperty(() => DepartmentQuotaPlanColor); }
            private set { SetProperty(() => DepartmentQuotaPlanColor, value); }
        }

        public static void BuildMetadata(MetadataBuilder<BacklogTaskAddToPlanViewModel> builder)
        {
            builder.Property(x => x.Estimate)
                .MatchesRule(x => x is > 0 and < 1000, () => "Значение должно быть в диапазоне 1..999");
            builder.Property(x => x.SelectedQuotaId)
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            _parameter = (BacklogTaskAddToPlanParameter)Parameter;

            await FetchQuotasAsync();

            await Task.WhenAll(FetchActiveBacklogTasksAsync(), FetchEmployeesAsync(), FetchMaxQuotaItAsync());

            Quotas = _quotas
                .Where(x => x.DepartmentId == _employeeDepartments[_parameter.BacklogTask.CreatedBy])
                .Select(x => new ComboBoxItem(x.Id, x.QuotaDate.ToString("dd.MM.yyyy")))
                .ToReadOnlyObservableCollection();

            Estimate = _parameter.BacklogTask.Estimate;
            SelectedQuotaId = _parameter.BacklogTask.QuotaId;

            await base.HandleLoadedAsync();

            Title = $"Добавление задачи №{_parameter.BacklogTask.Id} в план";

            CalculatePlans();
        }

        protected override Task HandleOkAsync()
        {
            CloseOk();

            return Task.CompletedTask;
        }

        private async Task FetchActiveBacklogTasksAsync()
        {
            BacklogFilteringItem filteringItem = new BacklogFilteringItem()
            {
                QuotaIds = _quotas.Select(x => x.Id).ToList()
            };

            PagedResult<BacklogTaskDto> backlogTasks = await WebClient.ExecuteApiRequestAsync(new QueryBacklogTasks(filteringItem));

            _backlogTasks = backlogTasks.Data.Where(x => x.Id != _parameter.BacklogTask.Id).ToArray();
        }

        private async Task FetchEmployeesAsync()
        {
            PagedResult<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true);

            _employeeDepartments = employees.Data.ToDictionary(x => x.Id, x => x.DepartmentId);
        }

        private async Task FetchMaxQuotaItAsync()
        {
            MaxQuotaIt maxQuotaIt = await WebClient.ExecuteApiRequestAsync(new GetMaxQuotaIt());

            _maxQuotaIt = maxQuotaIt.Value;
        }

        private async Task FetchQuotasAsync()
        {
            List<QuotaDto> quotas = await WebClient.ExecuteApiRequestAsync(new QueryQuotas());

            _quotas = quotas.Where(x => x.CreatedOn > DateTime.Now.AddDays(-60)).ToArray();
        }

        private void CalculatePlans()
        {
            if (SelectedQuotaId is null)
            {
                DepartmentQuotaPlanString = "-";
                TotalQuotaPlanString = "-";
                return;
            }

            int estimate = Estimate ?? 0;

            DateTime selectedQuotaDate = _quotas.First(x => x.Id == SelectedQuotaId.Value).QuotaDate;

            BacklogTaskDto[] currentQuotaPeriodBacklogTasks = _backlogTasks
                .Where(x => _quotas.First(z => z.Id == x.QuotaId!.Value).QuotaDate == selectedQuotaDate && x.Estimate.HasValue)
                .ToArray();

            int totalQuotaPlan = currentQuotaPeriodBacklogTasks.Sum(x => x.Estimate!.Value) + estimate;

            TotalQuotaPlanString = $"{totalQuotaPlan}/{_maxQuotaIt} ч.";

            TotalQuotaPlanColor = totalQuotaPlan > _maxQuotaIt ? RedColor : BlackColor;

            int departmentCurrentEstimate = currentQuotaPeriodBacklogTasks
                .Where(x => _employeeDepartments[x.CreatedBy] == _employeeDepartments[_parameter.BacklogTask.CreatedBy])
                .Sum(x => x.Estimate!.Value) + estimate;

            int departmentTotalEstimate = _quotas.First(x => x.Id == SelectedQuotaId.Value).Value;

            DepartmentQuotaPlanString = $"{departmentCurrentEstimate}/{departmentTotalEstimate} ч.";

            DepartmentQuotaPlanColor = departmentCurrentEstimate > departmentTotalEstimate ? RedColor : BlackColor;
        }
    }
}