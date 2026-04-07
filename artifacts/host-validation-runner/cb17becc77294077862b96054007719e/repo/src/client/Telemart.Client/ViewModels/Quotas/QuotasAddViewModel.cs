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
using Telemart.Client.Data.Requests.Features.Backlog;
using Telemart.Client.Data.Requests.Features.CompanyStructure;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Quotas;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Properties;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Backlog;
using Telemart.Client.TransferObjects.CompanyStructure;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Quotas;
using Telemart.Client.ViewModels.Backlog;
using Telemart.Client.ViewModels.Base;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Quotas
{
    public sealed class QuotasAddViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private readonly IMessenger _messenger;

        private IReadOnlyCollection<QuotaDto> _allQuotas;
        private IReadOnlyCollection<DepartmentDto> _allActiveDepartments;
        private IReadOnlyDictionary<int, EmployeeDto> _employees;
        private IReadOnlyCollection<BacklogTaskDto> _backlogTasksForLast12Quotas;
        private IReadOnlyCollection<BacklogTaskDto> _allBacklogTasks;
        private HashSet<int> _last12QuotaIds;

        public QuotasAddViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
            _messenger = messenger;

            HandleValueChangedCommand = new DelegateCommand<object>(HandleValueChanged);
        }

        public IDelegateCommand HandleValueChangedCommand { get; }

        public ReadOnlyObservableCollection<ComboBoxItem> Departments
        {
            get { return GetProperty(() => Departments); }
            set { SetProperty(() => Departments, value); }
        }

        public ReadOnlyObservableCollection<QuotaAddViewItem> QuotaAddViewItems
        {
            get { return GetProperty(() => QuotaAddViewItems); }
            set { SetProperty(() => QuotaAddViewItems, value); }
        }

        public QuotaAddViewItem SelectedQuotaAddViewItem
        {
            get { return GetProperty(() => SelectedQuotaAddViewItem); }
            set { SetProperty(() => SelectedQuotaAddViewItem, value); }
        }

        public DateTime QuotaDate
        {
            get { return GetProperty(() => QuotaDate); }
            set { SetProperty(() => QuotaDate, value, QuotaDateChanged); }
        }

        public int SumQuotas
        {
            get { return GetProperty(() => SumQuotas); }
            private set { SetProperty(() => SumQuotas, value); }
        }

        public int SumPlanQuotas
        {
            get { return GetProperty(() => SumPlanQuotas); }
            private set { SetProperty(() => SumPlanQuotas, value); }
        }

        public int MaxQuotaIt
        {
            get { return GetProperty(() => MaxQuotaIt); }
            private set { SetProperty(() => MaxQuotaIt, value); }
        }

        public static void BuildMetadata(MetadataBuilder<QuotasAddViewModel> builder)
        {
            builder.Property(x => x.QuotaDate)
                .MatchesRule(x => x > DateTime.Now.AddMonths(-1), () => "Можно выбрать только текущий месяц и позже")
                .Required(() => Resources.RequiredErrorMessage);
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(LoadDepartmentsAsync(), LoadQuotasAsync(), LoadMaxQuotaIdAsync(), LoadBacklogTasksAsync(), LoadEmployeesAsync());

            _last12QuotaIds = _allQuotas.OrderByDescending(x => x.QuotaDate).Take(12).Select(x => x.Id).ToHashSet();

            _backlogTasksForLast12Quotas = _allBacklogTasks.Where(x => x.QuotaId.HasValue && _last12QuotaIds.Contains(x.QuotaId.Value)).ToArray();

            QuotaDate = new DateTime(DateTime.Now.Year, DateTime.Now.AddMonths(1).Month, 1);

            HandleValueChanged(null);

            Title = "Создание квот";
        }

        protected override async Task HandleOkAsync()
        {
            if (QuotaAddViewItems?.All(x => x.NewValue >= 0) != true)
            {
                MessageFacadeService.ShowNotificationWarning("Не всем отделам проставлено значение");
                return;
            }

            if (QuotaAddViewItems.Sum(x => x.NewValue) > MaxQuotaIt || QuotaAddViewItems.Sum(x => x.PlanValue) > MaxQuotaIt)
            {
                MessageFacadeService.ShowNotificationError("Максимальное количество квот превышено");
                return;
            }

            QuotaCreateDto[] quotaCreateDtos = QuotaAddViewItems.Select(
                x => new QuotaCreateDto
                {
                    DepartmentId = x.DepartmentId,
                    Value = x.NewValue,
                    PlanValue = x.PlanValue
                }).ToArray();

            Result<List<QuotaDto>> result = await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new CreateQuotas(QuotaDate, quotaCreateDtos)),
                "создании квот",
                "Квоты созданы",
                this,
                true,
                confirmText: "Создать новые квоты");

            if (result?.IsSuccess == true)
            {
                _messenger.Send(new QuotasCreateMessage(result.Data.ToArray(), MessageType.Added));
                CloseOk();
            }
        }

        private void HandleValueChanged(object _ = null)
        {
            SumQuotas = QuotaAddViewItems?.Sum(x => x.NewValue) ?? 0;
            SumPlanQuotas = QuotaAddViewItems?.Sum(x => x.PlanValue) ?? 0;

            RaisePropertiesChanged(nameof(SumPlanQuotas), nameof(SumQuotas));
        }

        private async Task LoadDepartmentsAsync()
        {
            List<DepartmentDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryDepartments());

            _allActiveDepartments = dtos.Where(x => x.Active).ToArray();

            Departments = _allActiveDepartments
                .Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
        }

        private async Task LoadBacklogTasksAsync()
        {
            BacklogFilteringItem filter = new BacklogFilteringItem()
            {
                Statuses = new List<int>(
                    new[]
                    {
                        BacklogTaskState.Completed.Id,
                        BacklogTaskState.InProgress.Id,
                        BacklogTaskState.Realized.Id,
                        BacklogTaskState.Documented.Id
                    })
            };

            PagedResult<BacklogTaskDto> pagedResult = await WebClient.ExecuteApiRequestAsync(new QueryBacklogTasks(filter));

            _allBacklogTasks = pagedResult.Data;
        }

        private async Task LoadEmployeesAsync()
        {
            PagedResult<EmployeeDto> pagedData = await WebClient.ExecuteApiRequestAsync(new QueryEmployees());

            _employees = pagedData.Data.ToDictionary(x => x.Id);
        }

        private async Task LoadQuotasAsync()
        {
            List<QuotaDto> quotaDtos = await WebClient.ExecuteApiRequestAsync(new QueryQuotas());

            _allQuotas = quotaDtos.ToArray();
        }

        private async Task LoadMaxQuotaIdAsync()
        {
            MaxQuotaIt maxQuotaIt = await WebClient.ExecuteApiRequestAsync(new GetMaxQuotaIt());

            MaxQuotaIt = maxQuotaIt?.Value ?? 0;
        }

        private QuotaAddViewItem MapToQuotaAddViewItem(DepartmentDto departmentDto)
        {
            QuotaDto quotaDto = _allQuotas.OrderByDescending(x => x.QuotaDate).FirstOrDefault(x => x.DepartmentId == departmentDto.Id);

            return new QuotaAddViewItem(departmentDto.Id, quotaDto?.Value, quotaDto?.PlanValue ?? 0);
        }

        private void QuotaDateChanged()
        {
            QuotaAddViewItems = _allActiveDepartments
                .Where(x => _allQuotas.All(y => y.DepartmentId != x.Id || y.QuotaDate != QuotaDate))
                .Select(MapToQuotaAddViewItem)
                .ToReadOnlyObservableCollection();

            foreach (QuotaAddViewItem departmentQuota in QuotaAddViewItems)
            {
                departmentQuota.Duty = GetDutyForDepartment(departmentQuota.DepartmentId);

                departmentQuota.CalculatedNewValue = Math.Max(departmentQuota.PlanValue - departmentQuota.Duty, 0);
                departmentQuota.NewValue = departmentQuota.CalculatedNewValue;
            }

            HandleValueChanged();
        }

        private int GetDutyForDepartment(int departmentId)
        {
            BacklogTaskDto[] departmentTasks = _backlogTasksForLast12Quotas
                .Where(x => _employees[x.CreatedBy].DepartmentId == departmentId
                            && x.Estimate.HasValue
                            && x.QuotaId.HasValue)
                .ToArray();

            int totalDepartmentPlanForPeriod = _allQuotas
                .Where(x => x.QuotaDate <= QuotaDate && _last12QuotaIds.Contains(x.Id) && x.DepartmentId == departmentId)
                .Sum(x => x.PlanValue);

            return Math.Max(0, departmentTasks.Sum(x => x.Estimate!.Value) - totalDepartmentPlanForPeriod);
        }
    }
}