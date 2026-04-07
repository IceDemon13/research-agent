using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Utils;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.TradeIn;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.ViewModels.TradeIn
{
    public sealed class TradeInFilterViewModel : ViewModelBase, IDataErrorInfo
    {
        public TradeInFilterViewModel(IDictionaries dictionaries, IWebClient webClient)
        {
            Dictionaries = dictionaries;
            WebClient = webClient;

            Statuses.AddRange(Dictionaries.GetItems<TradeInState>());
            CancelProductCommand = new DelegateCommand(CleanProduct);
        }

        public IDelegateCommand CancelProductCommand { get; }

        public DateTime? CreatedBefore
        {
            get { return GetProperty(() => CreatedBefore); }
            set { SetProperty(() => CreatedBefore, value); }
        }

        public DateTime? CreatedAfter
        {
            get { return GetProperty(() => CreatedAfter); }
            set { SetProperty(() => CreatedAfter, value); }
        }

        public DateTime? EvaluatedOnAfter
        {
            get { return GetProperty(() => EvaluatedOnAfter); }
            set { SetProperty(() => EvaluatedOnAfter, value); }
        }

        public DateTime? EvaluatedOnBefore
        {
            get { return GetProperty(() => EvaluatedOnBefore); }
            set { SetProperty(() => EvaluatedOnBefore, value); }
        }

        public DateTime? CompletedOnAfter
        {
            get { return GetProperty(() => CompletedOnAfter); }
            set { SetProperty(() => CompletedOnAfter, value); }
        }

        public DateTime? CompletedOnBefore
        {
            get { return GetProperty(() => CompletedOnBefore); }
            set { SetProperty(() => CompletedOnBefore, value); }
        }

        public int? SelectedCreatedByEmployee
        {
            get { return GetProperty(() => SelectedCreatedByEmployee); }
            set { SetProperty(() => SelectedCreatedByEmployee, value); }
        }

        public int? SelectedEvaluatedByEmployee
        {
            get { return GetProperty(() => SelectedEvaluatedByEmployee); }
            set { SetProperty(() => SelectedEvaluatedByEmployee, value); }
        }

        public int? SelectedTestedByEmployee
        {
            get { return GetProperty(() => SelectedTestedByEmployee); }
            set { SetProperty(() => SelectedTestedByEmployee, value); }
        }

        public int? SelectedCompletedByEmployee
        {
            get { return GetProperty(() => SelectedCompletedByEmployee); }
            set { SetProperty(() => SelectedCompletedByEmployee, value); }
        }

        public ObservableCollection<TradeInState> SelectedStatuses
        {
            get { return GetProperty(() => SelectedStatuses); }
            set { SetProperty(() => SelectedStatuses, value); }
        }

        public ObservableCollection<TradeInIndicatorValueDto> SelectedClasses
        {
            get { return GetProperty(() => SelectedClasses); }
            set { SetProperty(() => SelectedClasses, value); }
        }

        public string Phone
        {
            get { return GetProperty(() => Phone); }
            set { SetProperty(() => Phone, value); }
        }

        public string Fio
        {
            get { return GetProperty(() => Fio); }
            set { SetProperty(() => Fio, value); }
        }

        public string ProductName
        {
            get { return GetProperty(() => ProductName); }
            set { SetProperty(() => ProductName, value); }
        }

        public int? ProductId
        {
            get { return GetProperty(() => ProductId); }
            set { SetProperty(() => ProductId, value); }
        }

        public string TradeInNumbers
        {
            get { return GetProperty(() => TradeInNumbers); }
            set { SetProperty(() => TradeInNumbers, value); }
        }

        public string ServiceRequestNumbers
        {
            get { return GetProperty(() => ServiceRequestNumbers); }
            set { SetProperty(() => ServiceRequestNumbers, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> Employees
        {
            get { return GetProperty(() => Employees); }
            set { SetProperty(() => Employees, value); }
        }

        public ReadOnlyObservableCollection<TradeInIndicatorValueDto> Classes
        {
            get { return GetProperty(() => Classes); }
            set { SetProperty(() => Classes, value); }
        }

        public ObservableRangeCollection<TradeInState> Statuses { get; } = new ObservableRangeCollection<TradeInState>();

        public string Error => string.Empty;

        public string this[string columnName] => IDataErrorInfoHelper.GetErrorText(this, columnName);

        public static void BuildMetadata(MetadataBuilder<TradeInFilterViewModel> builder)
        {
            builder.Property(x => x.TradeInNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
            builder.Property(x => x.ServiceRequestNumbers)
                .MatchesRegularExpression(@"^(\d+(,|,\d+)*)?$", () => "Допускаются только целыe числа, разделенные запятой");
        }

        public Task RefreshAsync()
        {
            SelectedStatuses = new ObservableCollection<TradeInState> { TradeInState.New, TradeInState.Evaluated, TradeInState.Received };

            return Task.WhenAll(RefreshEmployeesAsync(), RefreshTradeInIndicatorValuesAsync());
        }

        public TradeInFilteringItem GetFilteringItem()
        {
            return new TradeInFilteringItem()
            {
                Fio = Fio,
                Phone = Phone,
                ClassIds = string.Join(',', SelectedClasses?.Select(x => x.Id) ?? Array.Empty<int>()),
                StateIds = string.Join(',', SelectedStatuses?.Select(x => x.Id) ?? Array.Empty<int>()),
                Ids = TradeInNumbers,
                ServiceRequestIds = ServiceRequestNumbers,
                CreatedOnBefore = CreatedBefore,
                CreatedOnAfter = CreatedAfter,
                EvaluatedOnAfter = EvaluatedOnAfter,
                EvaluatedOnBefore = EvaluatedOnBefore,
                CompletedOnBefore = CompletedOnBefore,
                CompletedOnAfter = CompletedOnAfter,
                CreatedBy = SelectedCreatedByEmployee,
                EvaluatedBy = SelectedEvaluatedByEmployee,
                TestedBy = SelectedTestedByEmployee,
                CompletedBy = SelectedCompletedByEmployee,
                ProductId = ProductId
            };
        }

        public void ResetFilterValues()
        {
            CreatedBefore = null;
            CreatedAfter = null;
            EvaluatedOnBefore = null;
            EvaluatedOnAfter = null;
            CompletedOnAfter = null;
            CompletedOnBefore = null;
            SelectedCreatedByEmployee = null;
            SelectedEvaluatedByEmployee = null;
            SelectedTestedByEmployee = null;
            SelectedCompletedByEmployee = null;
            SelectedStatuses = new ObservableCollection<TradeInState> { TradeInState.New, TradeInState.Evaluated };
            SelectedClasses = new ObservableCollection<TradeInIndicatorValueDto>();
            Phone = null;
            Fio = null;
            ProductName = null;
            ProductId = null;
            TradeInNumbers = null;
            ServiceRequestNumbers = null;
        }

        private IDictionaries Dictionaries { get; }

        private IWebClient WebClient { get; }

        private async Task RefreshEmployeesAsync()
        {
            List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();

            Employees = employees.Where(x => x.Active)
                .Select(x => new ComboBoxItem(x.Id, x.Name))
                .OrderBy(x => x.DisplayValue)
                .ToReadOnlyObservableCollection();
        }

        private async Task RefreshTradeInIndicatorValuesAsync()
        {
            List<TradeInIndicatorValueDto> dtos = await WebClient.ExecuteApiRequestAsync(new QueryTradeInIndicatorValues());

            if (dtos?.Count > 0)
            {
                Classes = dtos.Where(x => x.IndicatorId == TradeInIndicator.Class.Id).ToReadOnlyObservableCollection();
            }
        }

        private void CleanProduct()
        {
            ProductId = null;
            ProductName = null;
        }
    }
}