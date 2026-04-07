using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AutoMapper;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.PivotGrid;
using Telemart.Client.Common;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.Services;
using Telemart.Client.Data.Requests.Features.Employee;
using Telemart.Client.Data.Requests.Features.Report;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.TransferObjects;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Reporting.Mvvm;
using Telemart.Client.ViewModels.Reporting.ViewItems;
using Telemart.Client.ViewModels.Validation;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.ViewModels.Reporting
{
    internal sealed class ReportEditorViewModel : TelemartEditorViewModelBase<ReportDto, ReportViewMessage, ReportViewItem>
    {
        public ReportEditorViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMapper mapper,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService, mapper, messenger)
        {
            MoveParameterUpCommand = new DelegateCommand<ReportParameterViewItem>(MoveParameterUp, x => x != null);
            MoveParameterDownCommand = new DelegateCommand<ReportParameterViewItem>(MoveParameterDown, x => x != null);
            RemoveParameterCommand = new DelegateCommand<ReportParameterViewItem>(RemoveParameter, x => x != null);

            MoveFieldUpCommand = new DelegateCommand<ReportFieldViewItem>(MoveFieldUp, x => x != null);
            MoveFieldDownCommand = new DelegateCommand<ReportFieldViewItem>(MoveFieldDown, x => x != null);
            RemoveFieldCommand = new DelegateCommand<ReportFieldViewItem>(RemoveField, x => x != null);
        }

        public ReportEditorViewModel()
        {
        }

        #region Commands

        public IDelegateCommand MoveParameterDownCommand { get; }

        public IDelegateCommand MoveParameterUpCommand { get; }

        public IDelegateCommand RemoveParameterCommand { get; }

        public IDelegateCommand MoveFieldDownCommand { get; }

        public IDelegateCommand MoveFieldUpCommand { get; }

        public IDelegateCommand RemoveFieldCommand { get; }

        #endregion

        #region INPC

        public ReadOnlyObservableCollection<string> AllRoles
        {
            get { return GetProperty(() => AllRoles); }
            private set { SetProperty(() => AllRoles, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> ReportDataBases
        {
            get { return GetProperty(() => ReportDataBases); }
            private set { SetProperty(() => ReportDataBases, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllEmployees
        {
            get { return GetProperty(() => AllEmployees); }
            private set { SetProperty(() => AllEmployees, value); }
        }

        public ReadOnlyObservableCollection<ComboBoxItem> AllReports
        {
            get { return GetProperty(() => AllReports); }
            private set { SetProperty(() => AllReports, value); }
        }

        public ReadOnlyObservableCollection<string> FieldAreas
        {
            get { return GetProperty(() => FieldAreas); }
            private set { SetProperty(() => FieldAreas, value); }
        }

        public ReadOnlyObservableCollection<string> FieldSummaryTypes
        {
            get { return GetProperty(() => FieldSummaryTypes); }
            private set { SetProperty(() => FieldSummaryTypes, value); }
        }

        public ReadOnlyObservableCollection<string> FieldGroupIntervals
        {
            get { return GetProperty(() => FieldGroupIntervals); }
            private set { SetProperty(() => FieldGroupIntervals, value); }
        }

        public ReadOnlyObservableCollection<string> FieldEntityTypes
        {
            get { return GetProperty(() => FieldEntityTypes); }
            private set { SetProperty(() => FieldEntityTypes, value); }
        }

        public ReadOnlyObservableCollection<string> FilterPopupModes
        {
            get { return GetProperty(() => FilterPopupModes); }
            private set { SetProperty(() => FilterPopupModes, value); }
        }

        public ReadOnlyObservableCollection<ReportParameterEditorType> ParameterEditorTypes
        {
            get { return GetProperty(() => ParameterEditorTypes); }
            private set { SetProperty(() => ParameterEditorTypes, value); }
        }

        public ReadOnlyObservableCollection<DataSourceViewItem> DataSources
        {
            get { return GetProperty(() => DataSources); }
            private set { SetProperty(() => DataSources, value); }
        }

        public bool ParametersHaveErrors => Model != null && Model.Parameters.Any(x => IDataErrorInfoHelper.HasErrors(x));

        public bool FieldsHaveErrors => Model != null && Model.Fields.Any(x => IDataErrorInfoHelper.HasErrors(x));

        #endregion INPC

        #region DialogSettings

        public override int Height => 768;

        public override int MinHeight => 400;

        public override int MinWidth => 600;

        public override int Width => 1024;

        #endregion

        protected override string CreatedActionMessage { get; } = "создан";

        protected override string EntityName { get; } = "Отчет";

        protected override string UpdatedActionMessage { get; } = "сохранен";

        protected override async Task<Result<ReportDto>> CreateEntityAsync()
        {
            ReportSaveDto dto = Mapper.Map<ReportSaveDto>(Model);
            ReportDto report = await WebClient.ExecuteReportApiRequestAsync(new CreateReport(dto));
            return new Result<ReportDto> { Data = report, Warnings = Array.Empty<string>() };
        }

        protected override object CreateEntityMessage(ReportDto dto, MessageType messageType)
        {
            return new ReportMessage(dto, messageType);
        }

        protected override Task<ReportDto> GetEntityAsync(int id)
        {
            return WebClient.ExecuteReportApiRequestAsync(new QueryReport(id));
        }

        protected override Task<LockResponse<ReportDto>> LockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override Task<LockResponse<ReportDto>> UnlockEntityAsync(int id)
        {
            throw new NotSupportedException();
        }

        protected override void SetCreateTitle()
        {
            Title = "Создание отчета";
        }

        protected override void SetEditTitle()
        {
            Title = $"Отчет \"{Model.Name}\" ({Model.Id.ToString(CultureInfo.InvariantCulture)})";
        }

        protected override bool IsValid(ReportViewItem model)
        {
            if (model.Fields.GroupBy(x => x.UniqueName).Any(x => x.Count() > 1))
            {
                MessageFacadeService.ShowNotificationWarning("Найдены колонки с одинаковым названием");
                return false;
            }

            if (model.Parameters.Any(x => x.SqlName.Contains(Constants.Sobaka)))
            {
                List<string> errors = new List<string>();

                foreach (ReportParameterViewItem parameter in model.Parameters)
                {
                    if (parameter.SqlName.First() != Constants.Sobaka)
                    {
                        errors.Add($"Параметр '{parameter.Name}' должен начинаться с {Constants.Sobaka}");
                        continue;
                    }

                    if (parameter.EditorType is ReportParameterEditorType.DecimalRange or ReportParameterEditorType.DateTimeRange)
                    {
                        string[] rangeSqlNames = parameter.SqlName
                            .Trim()
                            .Split(" ")
                            .Select(x => x.Trim())
                            .ToArray();

                        if (rangeSqlNames.Length != 2)
                        {
                            errors.Add($"Параметр '{parameter.Name}' является диапазонным, и должен содержать 2 переменные в формате: @FromDate1 @FromDate2");
                            continue;
                        }

                        if (rangeSqlNames.Any(x => x.First() != Constants.Sobaka))
                        {
                            errors.Add($"Параметр '{parameter.Name}' является диапазонным, обе переменные должны начинаться с {Constants.Sobaka}");
                        }
                    }
                }

                if (errors.Any())
                {
                    MessageFacadeService.ShowValidationResultView("Ошибки", errors.Select(x => new ValidationResultItem(x, true)).ToArray(), this);
                    return false;
                }
            }

            return base.IsValid(model);
        }

        protected override async Task<Result<ReportDto>> UpdateEntityAsync()
        {
            ReportSaveDto dto = Mapper.Map<ReportSaveDto>(Model);

            ReportDto report = await WebClient.ExecuteReportApiRequestAsync(new UpdateReport(dto.Id, dto));
            return new Result<ReportDto> { Data = report, Warnings = Array.Empty<string>() };
        }

        protected override IEnumerable<string> GetMembersToIgnore()
        {
            yield return nameof(ReportViewItem.EmployeeLockId);
            yield return nameof(ReportViewItem.EmployeeLockName);
        }

        protected override void AfterSetData()
        {
            foreach (ReportParameterViewItem x in Model.Parameters)
            {
                x.PropertyChanged += OnParameterPropertyChanged;
            }

            foreach (ReportFieldViewItem x in Model.Fields)
            {
                if (string.IsNullOrWhiteSpace(x.Area))
                {
                    x.Area = FieldArea.FilterArea.ToString("G");
                }

                if (string.IsNullOrWhiteSpace(x.SummaryType))
                {
                    x.SummaryType = FieldSummaryType.Count.ToString("G");
                }

                x.PropertyChanged += OnFieldPropertyChanged;
            }

            RaisePropertiesChanged(nameof(ParametersHaveErrors), nameof(FieldsHaveErrors));
        }

        protected override void BeforeSetData(ReportViewItem model, object dto)
        {
            if (Model != null)
            {
                foreach (ReportParameterViewItem x in Model.Parameters)
                {
                    x.PropertyChanged -= OnParameterPropertyChanged;
                }

                foreach (ReportFieldViewItem x in Model.Fields)
                {
                    x.PropertyChanged -= OnFieldPropertyChanged;
                }
            }
        }

        protected override async Task HandleLoadedAsync()
        {
            await Task.WhenAll(RefreshEmployeesAsync(), RefreshReportsAsync(), RefreshDataSourcesAsync(), RefreshReportDatabasesAsync());

            AllRoles = Dictionaries.GetItems<Role>().Select(x => x.Name).OrderBy(x => x).ToReadOnlyObservableCollection();

            FieldAreas = GetFieldAreas().Select(x => x.ToString("G")).ToReadOnlyObservableCollection();
            FieldSummaryTypes = GetFieldSummaryTypes().Select(x => x.ToString("G")).ToReadOnlyObservableCollection();
            FieldGroupIntervals = GetFieldGroupIntervals().Select(x => x.ToString("G")).ToReadOnlyObservableCollection();
            FieldEntityTypes = GetFieldEntityTypes().ToReadOnlyObservableCollection();
            FilterPopupModes = Enum.GetNames(typeof(DevExpress.Xpf.Grid.FilterPopupMode)).ToReadOnlyObservableCollection();
            ParameterEditorTypes = Enum.GetValues(typeof(ReportParameterEditorType)).Cast<ReportParameterEditorType>().ToReadOnlyObservableCollection();

            await base.HandleLoadedAsync();

            async Task RefreshReportDatabasesAsync()
            {
                List<ReportDatabaseDto> reportDatabases = await WebClient.ExecuteReportApiRequestAsync(new QueryReportDatabases());

                ReportDataBases = reportDatabases.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }

            async Task RefreshEmployeesAsync()
            {
                List<EmployeeDto> employees = await WebClient.ExecuteApiRequestAsync(new QueryEmployees(), true).GetPagedResultDataAsync();
                AllEmployees = employees.Where(x => x.Active).OrderBy(x => x.Name).Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }

            async Task RefreshReportsAsync()
            {
                List<ReportSimpleDto> reports = await WebClient.ExecuteReportApiRequestAsync(new QueryReports());
                AllReports = reports.Select(x => new ComboBoxItem(x.Id, x.Name)).ToReadOnlyObservableCollection();
            }

            async Task RefreshDataSourcesAsync()
            {
                List<DataSourceDto> dataSources = await WebClient.ExecuteReportApiRequestAsync(new QueryDataSources());
                DataSources = dataSources
                    .OrderBy(x => x.Name)
                    .Select(x => Mapper.Map<DataSourceViewItem>(x))
                    .ToReadOnlyObservableCollection();
            }

            IEnumerable<FieldArea> GetFieldAreas()
            {
                yield return FieldArea.ColumnArea;
                yield return FieldArea.RowArea;
                yield return FieldArea.FilterArea;
                yield return FieldArea.DataArea;
            }

            IEnumerable<FieldGroupInterval> GetFieldGroupIntervals()
            {
                yield return FieldGroupInterval.Date;
                yield return FieldGroupInterval.DateDay;
                yield return FieldGroupInterval.DateMonth;
                yield return FieldGroupInterval.DateYear;
            }

            IEnumerable<FieldSummaryType> GetFieldSummaryTypes()
            {
                yield return FieldSummaryType.Average;
                yield return FieldSummaryType.Count;
                yield return FieldSummaryType.Max;
                yield return FieldSummaryType.Min;
                yield return FieldSummaryType.Sum;
            }
        }

        protected override Task<bool> SaveAsync()
        {
            for (int i = Model.Parameters.Count - 1; i >= 0; i--)
            {
                if (IDataErrorInfoHelper.HasErrors(Model.Parameters[i]))
                {
                    Model.Parameters.RemoveAt(i);
                }
            }

            for (int i = Model.Fields.Count - 1; i >= 0; i--)
            {
                if (IDataErrorInfoHelper.HasErrors(Model.Fields[i]))
                {
                    Model.Fields.RemoveAt(i);
                }
            }

            if (Model.Parameters
                .Where(x => x.EditorType == ReportParameterEditorType.TokenComboBox || x.EditorType == ReportParameterEditorType.CheckedTokenComboBox)
                .Any(x => x.DataSourceId == null))
            {
                MessageFacadeService.ShowNotificationWarning("Заполните DataSource(s) во вкладке \"Параметры\"");
                return Task.FromResult(false);
            }

            return base.SaveAsync();
        }

        private static IEnumerable<string> GetFieldEntityTypes()
        {
            return typeof(ReportEntityTypes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Select(x => x.GetValue(null))
                .Cast<string>()
                .OrderBy(x => x);
        }

        private void OnParameterPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(ParametersHaveErrors));
            RefreshIsChanged();
        }

        private void OnFieldPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(FieldsHaveErrors));
            RefreshIsChanged();
        }

        private void MoveParameterDown(ReportParameterViewItem obj)
        {
            Model.Parameters.ModeItemDown(obj);
            RefreshIsChanged();
        }

        private void MoveParameterUp(ReportParameterViewItem obj)
        {
            Model.Parameters.ModeItemUp(obj);
            RefreshIsChanged();
        }

        private void RemoveParameter(ReportParameterViewItem obj)
        {
            if (MessageFacadeService.Confirm("Вы уверены?"))
            {
                Model.Parameters.Remove(obj);
                RefreshIsChanged();
            }
        }

        private void MoveFieldDown(ReportFieldViewItem obj)
        {
            Model.Fields.ModeItemDown(obj);
            RefreshIsChanged();
        }

        private void MoveFieldUp(ReportFieldViewItem obj)
        {
            Model.Fields.ModeItemUp(obj);
            RefreshIsChanged();
        }

        private void RemoveField(ReportFieldViewItem obj)
        {
            if (MessageFacadeService.Confirm("Вы уверены?"))
            {
                Model.Fields.Remove(obj);
                RefreshIsChanged();
            }
        }

        private void RefreshIsChanged()
        {
            RaisePropertyChanged(nameof(IsChanged));
        }
    }
}