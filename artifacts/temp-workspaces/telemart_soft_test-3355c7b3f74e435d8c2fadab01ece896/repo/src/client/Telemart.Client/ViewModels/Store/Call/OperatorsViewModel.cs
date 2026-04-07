using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.Asterisk;
using Telemart.Client.Common.ErrorHandler;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Asterisk;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Setting;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.Asterisk;
using Telemart.Client.ViewModels.Base;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class OperatorsViewModel : TelemartDialogViewModelBase
    {
        private readonly IErrorHandler _errorHandler;
        private IReadOnlyCollection<AsteriskPresenceDto> _asteriskPresenceStatuses;
        private IReadOnlyCollection<AsteriskDndDto> _asteriskDndStatuses;
        private IDictionary<int, TimeSpan?> _originalMaxBusyTimes;
        private short _busySecondsAfterCall;

        public OperatorsViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IErrorHandler errorHandler)
            : base(webClient, dictionaries, messageFacadeService)
        {
            _errorHandler = errorHandler;
            RefreshCommand = new AsyncCommand(RefreshAsync);
        }

        public ObservableCollection<OperatorViewItem> OperatorViewItems
        {
            get { return GetProperty(() => OperatorViewItems); }
            set { SetProperty(() => OperatorViewItems, value); }
        }

        public OperatorViewItem SelectedOperatorViewItem
        {
            get { return GetProperty(() => SelectedOperatorViewItem); }
            set { SetProperty(() => SelectedOperatorViewItem, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public short QuantitySeconds
        {
            get { return GetProperty(() => QuantitySeconds); }
            set { SetProperty(() => QuantitySeconds, value); }
        }

        public bool IsEditingMaxBusyTime => WebClient.IsOperationAllowed(BusinessOperation.AllowChangeMaxBusyTime);

        public bool AsteriskStatusVisible => WebClient.AuthenticatedEmployee.Roles.Contains(Role.Admin.Name);

        public IAsyncCommand RefreshCommand { get; }

        public override int Width => 1280;

        public override int MinWidth => 800;

        public override int MaxWidth => 1920;

        public override int Height => 720;

        public override int MinHeight => 500;

        public override int MaxHeight => 1080;

        public static void BuildMetadata(MetadataBuilder<OperatorsViewModel> builder)
        {
            builder.Property(x => x.QuantitySeconds).MatchesRule(x => x >= 0 & x <= 60, () => "Допустимые значения 0...60 сек");
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(LoadEmployeesAsync(), LoadAsteriskStatusesAsync(), LoadBusySecondsAfterCallAsync());

            await RefreshAsync();

            Title = "Операторы";
        }

        protected override async Task HandleOkAsync()
        {
            bool isChangeMaxBusyTimeEnployees = OperatorViewItems.Any(x => x.EmployeeId.HasValue && (!_originalMaxBusyTimes.TryGetValue(x.EmployeeId.Value, out TimeSpan? span) || x.MaxBusyTime != span));

            if (IsEditingMaxBusyTime
                && (isChangeMaxBusyTimeEnployees || _busySecondsAfterCall != QuantitySeconds)
                && MessageFacadeService.Confirm("Сохранить изменения?"))
            {
                if (isChangeMaxBusyTimeEnployees)
                {
                    await UpdateAsteriskEmployeeAsync();
                }

                if (_busySecondsAfterCall != QuantitySeconds)
                {
                    await UpdateBusySecondsAfterCallAsync();
                }
            }

            CloseOk();
        }

        private async Task UpdateAsteriskEmployeeAsync()
        {
            AsteriskEmployeeSaveDto[] employeeMaxBusyTimes = OperatorViewItems.Where(
                    x => x.EmployeeId.HasValue
                         && (!_originalMaxBusyTimes.TryGetValue(x.EmployeeId.Value, out TimeSpan? span)
                             || x.MaxBusyTime != span))
                .Select(x => new AsteriskEmployeeSaveDto()
                    {
                        EmployeeId = x.EmployeeId.Value,
                        MaxBusyTime = x.MaxBusyTime
                    }).ToArray();

            await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteCallApiRequestAsync(new UpdateAsteriskEmployees(employeeMaxBusyTimes)),
                "при сохранении максимального времени",
                $"Максимальное время занятости сохранено",
                this,
                true,
                showNotification: true);
        }

        private async Task UpdateBusySecondsAfterCallAsync()
        {
            await _errorHandler.HandleErrorsAsync(
                _ => WebClient.ExecuteApiRequestAsync(new UpdateBusySecondsAfterCall(QuantitySeconds)),
                "при сохранении кол-во секунд на статус Занят после звонка",
                $"Кол-во секунд на статус Занят после звонка сохранено",
                this,
                true,
                showNotification: true);
        }

        private async Task RefreshAsync()
        {
            await LoadOperatorsAsync();

            RaisePropertiesChanged(nameof(IsEditingMaxBusyTime), nameof(AsteriskStatusVisible));
        }

        private async Task LoadEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            employees = employees.OrderBy(x => x.Name).ToList();

            Employees = employees.Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();
        }

        private async Task LoadOperatorsAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(AsteriskConstants.AsteriskAccount, AsteriskConstants.CallCenterDepartmentId)).GetPagedResultDataAsync();

            List<AsteriskEmployeeStatusDto> asteriskEmployeeStatuses = await WebClient.ExecuteCallApiRequestAsync(new QueryAsteriskEmployeeStatuses());

            _originalMaxBusyTimes = asteriskEmployeeStatuses.GroupBy(x => x.EmployeeId)
                .ToDictionary(x => x.Key, y => y.First().MaxBusyTime);

            OperatorViewItems = employees.Where(x => x.Active && x.PositionId != AsteriskConstants.ManagerCallCenterId)
                .Select(x => Map(x, asteriskEmployeeStatuses.FirstOrDefault(y => y.EmployeeId == x.Id)))
                .ToObservableCollection();
        }

        private async Task LoadBusySecondsAfterCallAsync()
        {
            if (!WebClient.IsOperationAllowed(BusinessOperation.AllowChangeMaxBusyTime))
            {
                return;
            }

            string busySecondsAfterCall = await WebClient.ExecuteApiRequestAsync(new QueryBusySecondsAfterCall());

            if (short.TryParse(busySecondsAfterCall, out short item))
            {
                _busySecondsAfterCall = item;

                QuantitySeconds = _busySecondsAfterCall;
            }
        }

        private async Task LoadAsteriskStatusesAsync()
        {
            _asteriskPresenceStatuses = await WebClient.ExecuteCallApiRequestAsync(new QueryAsteriskPresenceStatuses());
            _asteriskDndStatuses = await WebClient.ExecuteCallApiRequestAsync(new QueryAsteriskDndStatuses());
        }

        private OperatorViewItem Map(EmployeeDto asteriskEmployee, AsteriskEmployeeStatusDto asteriskEmployeeStatusDto)
        {
            TimeSpan? maxBusyTime = TimeSpan.Zero;

            if (IsEditingMaxBusyTime)
            {
                maxBusyTime = asteriskEmployeeStatusDto?.MaxBusyTime ?? TimeSpan.Zero;
            }
            else
            {
                maxBusyTime = WebClient.AuthenticatedEmployee.Id == asteriskEmployee.Id
                    ? asteriskEmployeeStatusDto?.MaxBusyTime
                    : TimeSpan.Zero;
            }

            return new OperatorViewItem
            {
                EmployeeId = asteriskEmployee.Id,
                Position = asteriskEmployee.Position,
                Status = asteriskEmployeeStatusDto != null ? _asteriskDndStatuses.FirstOrDefault(x => x.Name == asteriskEmployeeStatusDto.Dnd)?.DisplayText : string.Empty,
                AsteriskStatus = asteriskEmployeeStatusDto != null ? MapStatus(asteriskEmployeeStatusDto.Presence, asteriskEmployeeStatusDto.Dnd) : string.Empty,
                LastModifiedOn = asteriskEmployeeStatusDto?.ModifiedOn,
                LastModifiedBy = asteriskEmployeeStatusDto?.ModifiedBy,
                AllowChangeMaxBusyTimeByEmployee = IsEditingMaxBusyTime,
                MaxBusyTime = maxBusyTime
            };
        }

        private string MapStatus(string presense, string dnd)
        {
            return $"{_asteriskDndStatuses.FirstOrDefault(x => x.Name == dnd)?.DisplayText ?? dnd} ({_asteriskPresenceStatuses.FirstOrDefault(x => x.Name == presense)?.DisplayText ?? presense})";
        }
    }
}