using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.PivotGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telemart.Client.Common.Behaviors;
using Telemart.Client.Common.Messages;
using Telemart.Client.Common.MvvmEnhancements;
using Telemart.Client.Common.MvvmEnhancements.Grid;
using Telemart.Client.Common.Services;
using Telemart.Client.Common.Utils;
using Telemart.Client.Core.Exceptions;
using Telemart.Client.Data.Requests.Features.Report;
using Telemart.Client.Data.Requests.Features.Report.TransferObjects;
using Telemart.Client.Data.WebClient;
using Telemart.Client.Data.WebClient.Security;
using Telemart.Client.Dictionaries;
using Telemart.Client.Extensions;
using Telemart.Client.Properties;
using Telemart.Client.ViewComponents.Editors;
using Telemart.Client.ViewModels.Backlog;
using Telemart.Client.ViewModels.Base;
using Telemart.Client.ViewModels.Reporting.Mvvm;
using Telemart.Client.ViewModels.Reporting.ViewItems;
using Telemart.Client.ViewModels.Validation;

namespace Telemart.Client.ViewModels.Reporting
{
    internal sealed class ReportViewModel : TelemartViewModelBase, ISupportHotkeys
    {
        private readonly Guid instanceId = Guid.NewGuid();

        private string defaultGridLayout = string.Empty;
        private string defaultPivotGridLayout = string.Empty;

        private GridControl gridControl;
        private PivotGridControl pivotGridControl;

        private IReadOnlyCollection<ReportFieldDto> fields;

        private int reportId;

        public ReportViewModel(
            IWebClient webClient,
            IDictionaries dictionaries,
            IMessageFacadeService messageFacadeService,
            IMessenger messenger)
            : base(webClient, dictionaries, messageFacadeService)
        {
            Messenger = messenger;

            RefreshCommand = new AsyncCommand(RefreshAsync);
            HandleRowDoubleClickCommand = new DelegateCommand<RowDoubleClickEventArgs>(HandleRowDoubleClick);
            LoadPlainReportLayoutsCommand = new DelegateCommand(LoadPlainReportLayouts);
            SavePlainReportLayoutsCommand = new DelegateCommand(SavePlainReportLayouts);
            LoadAnalyzeReportLayoutsCommand = new DelegateCommand(LoadAnalyzeReportLayouts);
            SaveAnalyzeReportLayoutsCommand = new DelegateCommand(SaveAnalyzeReportLayouts);
            LegendCommand = new DelegateCommand(Legend);
            EditCommand = new DelegateCommand(Edit, () => IsAllowReportSettings);
            LoadGridCommand = new DelegateCommand<GridControl>(x => gridControl = x);
            LoadPivotGridCommand = new DelegateCommand<PivotGridControl>(x => pivotGridControl = x);

            Columns = new ObservableCollection<GridColumnItem>();
            FieldItems = new ObservableCollection<PivotGridFieldItem>();

            Messenger.Register<LoadReportLayoutMessage>(this, OnLoadReportLayout);

            DataSourceItems = new ObservableRangeCollection<object>();
        }

        public ReportViewModel()
        {
        }

        #region Commands

        public IAsyncCommand RefreshCommand { get; }

        public IDelegateCommand LoadPlainReportLayoutsCommand { get; }

        public IDelegateCommand SavePlainReportLayoutsCommand { get; }

        public IDelegateCommand LoadAnalyzeReportLayoutsCommand { get; }

        public IDelegateCommand SaveAnalyzeReportLayoutsCommand { get; }

        public IDelegateCommand HandleRowDoubleClickCommand { get; }

        public IDelegateCommand LegendCommand { get; }

        public IDelegateCommand EditCommand { get; }

        public IDelegateCommand LoadGridCommand { get; }

        public IDelegateCommand LoadPivotGridCommand { get; }

        #endregion

        #region INPC

        public ObservableCollection<GridColumnItem> Columns
        {
            get { return GetProperty(() => Columns); }
            private set { SetProperty(() => Columns, value); }
        }

        public ObservableRangeCollection<object> DataSourceItems
        {
            get { return GetProperty(() => DataSourceItems); }
            private set { SetProperty(() => DataSourceItems, value); }
        }

        public ObservableCollection<PivotGridFieldItem> FieldItems
        {
            get { return GetProperty(() => FieldItems); }
            private set { SetProperty(() => FieldItems, value); }
        }

        public bool IsAnalyzeMode
        {
            get { return GetProperty(() => IsAnalyzeMode); }
            set { SetProperty(() => IsAnalyzeMode, value, ModeChanged); }
        }

        public bool IsColumnChooserVisible
        {
            get { return GetProperty(() => IsColumnChooserVisible); }
            set { SetProperty(() => IsColumnChooserVisible, value); }
        }

        public bool IsFieldListVisible
        {
            get { return GetProperty(() => IsFieldListVisible); }
            set { SetProperty(() => IsFieldListVisible, value); }
        }

        public bool IsPanelClosed
        {
            get { return GetProperty(() => IsPanelClosed); }
            set { SetProperty(() => IsPanelClosed, value); }
        }

        public ReadOnlyObservableCollection<ReportParameter> Parameters
        {
            get { return GetProperty(() => Parameters); }
            private set { SetProperty(() => Parameters, value); }
        }

        public string GridLayout
        {
            get { return GetProperty(() => GridLayout); }
            set { SetProperty(() => GridLayout, value); }
        }

        public string PivotGridLayout
        {
            get { return GetProperty(() => PivotGridLayout); }
            set { SetProperty(() => PivotGridLayout, value); }
        }

        public ObservableCollection<FormattingRuleItem> FormattingRules
        {
            get { return GetProperty(() => FormattingRules); }
            private set { SetProperty(() => FormattingRules, value); }
        }

        #endregion

        public bool IsAllowReportSettings => WebClient.IsOperationAllowed(BusinessOperation.AllowReportSettings) || WebClient.IsOperationAllowed(BusinessOperation.AllowViewReportSettings);

        private IMessenger Messenger { get; }

        private IDocumentManagerService SizeableDialogDocumentManagerService => GetService<IDocumentManagerService>("SizeableDialogDocumentManagerService", ServiceSearchMode.PreferParents);

        private IDocumentManagerService NotModalSizeableDocumentManagerService => GetService<IDocumentManagerService>("NotModalSizeableDocumentManagerService", ServiceSearchMode.PreferParents);

        public bool HandleHotkey(HotkeyMessage msg)
        {
            bool handled = false;

            if (msg.ModifierKeys == ModifierKeys.Alt)
            {
                switch (msg.Key)
                {
                    case Key.L:
                        IsPanelClosed = !IsPanelClosed;
                        handled = true;
                        break;
                }
            }
            else
            {
                switch (msg.HotkeyMessageType)
                {
                    case HotkeyMessageType.Refresh:
                        RefreshCommand.Execute(null);
                        handled = true;
                        break;
                    case HotkeyMessageType.ShowColumnChooser:
                        if (IsAnalyzeMode)
                        {
                            IsFieldListVisible = !IsFieldListVisible;
                        }
                        else
                        {
                            IsColumnChooserVisible = !IsColumnChooserVisible;
                        }

                        handled = true;
                        break;
                }
            }

            return handled;
        }

        protected override async Task HandleLoadedAsync()
        {
            if (reportId <= 0)
            {
                IsPanelClosed = false;

                reportId = (int)Parameter;

                try
                {
                    ReportParamsDto report = await WebClient.ExecuteReportApiRequestAsync(new PreExecuteReport(reportId));

                    SetFields(report.Fields);

                    Parameters = report.Parameters.Select(MapParameter).ToReadOnlyObservableCollection();

                    ReportParameter firstParameter = Parameters.FirstOrDefault();

                    if (firstParameter != null && firstParameter.EditorType == ReportParameterEditorType.DateTimeRange)
                    {
                        DateTimeRange range = (DateTimeRange)firstParameter.Value;
                        range.PredefinedPeriod = DatePeriodEditValue.CurrentMonth;
                    }

                    defaultGridLayout = SaveLayoutToString(gridControl.SaveLayoutToStream);
                    defaultPivotGridLayout = SaveLayoutToString(pivotGridControl.SaveLayoutToStream);
                }
                catch (UnexpectedSatusException exception)
                {
                    MessageFacadeService.ShowNotificationError(Resources.ErrorExecutingOperation);
                    ShowValidationResultView("Ошибки при выполнении операции", exception.GetErrorItems());
                }
            }
        }

        private static IEnumerable<object> ConvertToDataSourceItems(IReadOnlyCollection<object> reportDataObjects)
        {
            Dictionary<string, Type> propertiesData = reportDataObjects.Select(JObject.FromObject)
                .SelectMany(x => x.Properties())
                .Where(x => x.Type == JTokenType.Property)
                .Select(x => new { x.Name, Type = ConvertType(x.Value.Type) })
                .GroupBy(x => x.Name)
                .Select(g => new { Name = g.Key, Type = g.FirstOrDefault(y => y.Type != null)?.Type ?? typeof(string) })
                .ToDictionary(x => x.Name, x => x.Type);

            foreach (object obj in reportDataObjects)
            {
                JObject jObj = JObject.FromObject(obj);

                IDictionary<string, object> target = new ExpandoObject();

                foreach (JProperty jProperty in jObj.Properties().Where(x => x.Type == JTokenType.Property))
                {
                    Type propType = propertiesData[jProperty.Name];
                    object propValue = jProperty.Value.ToObject(propType);

                    target[jProperty.Name] = propValue;
                }

                yield return target;
            }
        }

        private static Type ConvertType(JTokenType valueType)
        {
            Type type = valueType switch
            {
                JTokenType.String => typeof(string),
                JTokenType.Integer => typeof(long?),
                JTokenType.Float => typeof(decimal?),
                JTokenType.Boolean => typeof(bool?),
                JTokenType.Date => typeof(DateTime?),
                JTokenType.TimeSpan => typeof(TimeSpan?),
                JTokenType.Guid => typeof(Guid?),
                _ => null
            };
            return type;
        }

        private static GridColumnItem MapColumn(ReportFieldDto x)
        {
            GridColumnItem column = new GridColumnItem(x.FieldName, x.Caption, false);

            if (!string.IsNullOrWhiteSpace(x.Description))
            {
                column.HeaderToolTip = x.Description;
            }

            if (!string.IsNullOrWhiteSpace(x.DisplayFormat))
            {
                column.DisplayFormat = x.DisplayFormat;
            }

            return column;
        }

        private static PivotGridFieldItem MapField(ReportFieldDto source)
        {
            return MapField(source, new PivotGridFieldItem());
        }

        private static PivotGridFieldItem MapField(ReportFieldDto source, PivotGridFieldItem target)
        {
            target.UniqueName = string.IsNullOrWhiteSpace(source.UniqueName)
                ? $"field{source.FieldName}"
                : $"field{source.UniqueName}";
            target.FieldName = source.FieldName;
            target.Caption = source.Caption;
            target.Description = !string.IsNullOrWhiteSpace(source.Description) ? source.Description : source.Caption;
            target.Area = (FieldArea)Enum.Parse(typeof(FieldArea), source.Area);

            if (!string.IsNullOrWhiteSpace(source.CellFormat))
            {
                target.CellFormat = source.CellFormat;
            }

            if (!string.IsNullOrWhiteSpace(source.GroupInterval))
            {
                target.GroupInterval = (FieldGroupInterval)Enum.Parse(typeof(FieldGroupInterval), source.GroupInterval);
            }

            if (!string.IsNullOrWhiteSpace(source.SummaryType))
            {
                target.SummaryType = (FieldSummaryType)Enum.Parse(typeof(FieldSummaryType), source.SummaryType);
            }
            else
            {
                target.SummaryType = FieldSummaryType.Sum;
            }

            return target;
        }

        private static ReportParameter MapParameter(ReportProcessedParameterDto source)
        {
            return MapParameter(source, new ReportParameter());
        }

        private static ReportParameter MapParameter(ReportProcessedParameterDto source, ReportParameter target)
        {
            target.Label = source.Name;
            target.EditorType = (ReportParameterEditorType)source.EditorType;
            target.ComboBoxItems = source.DataSource?.Select(y => new ComboBoxItem(y.Id, y.Name)).ToReadOnlyObservableCollection();

            switch (target.EditorType)
            {
                case ReportParameterEditorType.DateTimeRange:
                    target.Value = new DateTimeRange();
                    break;
                case ReportParameterEditorType.DecimalRange:
                    target.Value = new DecimalRange();
                    break;
            }

            return target;
        }

        private static string SaveLayoutToString(Action<MemoryStream> saveToStreamAction)
        {
            using MemoryStream layoutStream = new MemoryStream();
            using StreamReader streamReader = new StreamReader(layoutStream);
            saveToStreamAction(layoutStream);

            layoutStream.Position = 0;

            return streamReader.ReadToEnd();
        }

        private void ModeChanged()
        {
            IsColumnChooserVisible = IsColumnChooserVisible && !IsAnalyzeMode;
            IsFieldListVisible = IsFieldListVisible && IsAnalyzeMode;
        }

        private async Task RefreshAsync()
        {
            DataSourceItems.Clear();

            try
            {
                ReportParameterValueDto[] parameterValues = GetParameterValues().ToArray();

                if (!WebClient.WorkPlaceId.HasValue)
                {
                    MessageFacadeService.ShowNotificationWarning("Отсутствует значение рабочего места");
                    return;
                }

                ExecuteReport gatewayRequest = new ExecuteReport(reportId, WebClient.WorkPlaceId.Value, parameterValues);

                List<object> result = await WebClient.ExecuteReportApiRequestAsync(gatewayRequest);

                DataSourceItems = ConvertToDataSourceItems(result).ToObservableRangeCollection();

                foreach (GridColumnItem column in Columns)
                {
                    ReportFieldDto field = fields.First(f => string.Equals(f.FieldName, column.FieldName, StringComparison.Ordinal));

                    if (Enum.TryParse(field.FilterPopupMode, false, out DevExpress.Xpf.Grid.FilterPopupMode filterPopupMode))
                    {
                        column.FilterPopupMode = filterPopupMode;
                    }
                    else
                    {
                        object dataSourceItem = DataSourceItems.FirstOrDefault();

                        if (dataSourceItem != null)
                        {
                            PropertyInfo pi = dataSourceItem
                                .GetType()
                                .GetProperty(column.FieldName, BindingFlags.Public | BindingFlags.Instance);

                            if (pi?.PropertyType == typeof(string))
                            {
                                column.FilterPopupMode = DevExpress.Xpf.Grid.FilterPopupMode.CheckedList;
                            }
                        }
                    }
                }
            }
            catch (UnexpectedSatusException exception)
            {
                MessageFacadeService.ShowNotificationError(exception.Args.Error.ErrorMessage);
            }
            catch (UnexpectedErrorException)
            {
                MessageFacadeService.ShowNotificationError(Resources.ServerConnectError);
            }
            catch (Exception)
            {
                MessageFacadeService.ShowNotificationError(Resources.ErrorDuringDataLoading);
            }
        }

        private void SetFields(IReadOnlyCollection<ReportFieldDto> reportFields)
        {
            if (fields == null || fields.Except(reportFields).Any() || reportFields.Except(fields).Any())
            {
                fields = reportFields;

                FormattingRules = null;
                Columns.Clear();
                FieldItems.Clear();

                List<FormattingRuleItem> formattingRules = new List<FormattingRuleItem>();

                foreach (ReportFieldDto x in fields)
                {
                    Columns.Add(MapColumn(x));
                    FieldItems.Add(MapField(x));

                    if (!string.IsNullOrWhiteSpace(x.CellFormat) && x.CellFormat.StartsWith("N"))
                    {
                        formattingRules.Add(new FormattingRuleItem(x.FieldName, $"[{x.FieldName}] < 0", false));
                    }
                }

                if (formattingRules.Any())
                {
                    FormattingRules = formattingRules.ToObservableCollection();
                }
            }
        }

        private void LoadPlainReportLayouts()
        {
            ManagePlainReportLayouts(true);
        }

        private void SavePlainReportLayouts()
        {
            ManagePlainReportLayouts(false);
        }

        private void ManagePlainReportLayouts(bool isLoad)
        {
            string layout = SaveLayoutToString(gridControl.SaveLayoutToStream);

            SizeableDialogDocumentManagerService.ShowView<ReportLayoutsViewModel>(
                new ReportLayoutsParameter(reportId, instanceId, ReportLayoutType.Grid, isLoad, layout, GetLayoutParameters().ToArray()),
                this);
        }

        private void LoadAnalyzeReportLayouts()
        {
            ManageAnalyzeReportLayouts(true);
        }

        private void SaveAnalyzeReportLayouts()
        {
            ManageAnalyzeReportLayouts(false);
        }

        private void ManageAnalyzeReportLayouts(bool isLoad)
        {
            string layout = SaveLayoutToString(pivotGridControl.SaveLayoutToStream);

            SizeableDialogDocumentManagerService.ShowView<ReportLayoutsViewModel>(
                new ReportLayoutsParameter(reportId, instanceId, ReportLayoutType.PivotGrid, isLoad, layout, GetLayoutParameters().ToArray()),
                this);
        }

        private void Legend()
        {
            List<ReportLegendViewItem> items = Columns
                .Select(c => new ReportLegendViewItem { Name = c.Header, Description = c.HeaderToolTip })
                .ToList();

            NotModalSizeableDocumentManagerService.ShowView<ReportLegendViewModel>(items, this);
        }

        private void HandleRowDoubleClick(RowDoubleClickEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            TableViewHitInfo hitInfo = (TableViewHitInfo)e.HitInfo;

            if (!hitInfo.InRowCell)
            {
                return;
            }

            TableView tableView = (TableView)e.Source;
            string text = tableView.Grid.GetCellDisplayText(e.Source.FocusedRowHandle, hitInfo.Column);

            if (!string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    ReportFieldDto field = fields.First(x => x.FieldName == hitInfo.Column.FieldName);

                    switch (field.EntityType)
                    {
                        case ReportEntityTypes.Order:
                            Messenger.Send(new OrderEditViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.Call:
                            Messenger.Send(new CallViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.Contractor:
                            Messenger.Send(new ContractorViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.Employee:
                            Messenger.Send(new EmployeeViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.Movement:
                            Messenger.Send(new MovementViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.Organization:
                            Messenger.Send(new OrganizationViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.ServiceRequest:
                            Messenger.Send(new ServiceRequestViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.ServiceRepair:
                            Messenger.Send(new ServiceRepairViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.ServiceProduct:
                            Messenger.Send(new ServiceProductViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.ServiceCenter:
                            Messenger.Send(new ServiceCenterViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.ServiceInvoice:
                            Messenger.Send(new ServiceInvoiceViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.Invoice:
                            Messenger.Send(new InvoiceEditViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.NpScanSheet:
                            Messenger.Send(new NpScanSheetViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.BacklogTask:
                            Messenger.Send(new BacklogTaskViewMessage(int.Parse(text)));
                            break;
                        case ReportEntityTypes.NpBillDocument:
                            Messenger.Send(new NpBillDocumentViewMessage(int.Parse(text)));
                            break;
                        default:
                            Clipboard.SetDataObject(text);
                            MessageFacadeService.ShowNotificationInfo("Скопировано в буфер");
                            break;
                    }
                }
                catch (FormatException)
                {
                    MessageFacadeService.ShowNotificationWarning("Не удалось преобразовать строку в число");
                }
            }
        }

        private void OnLoadReportLayout(LoadReportLayoutMessage message)
        {
            if (message.ReportId == reportId && message.ReportEditorId == instanceId)
            {
                LoadReportLayoutView(message);
                LoadReportLayoutParameters(message);

                MessageFacadeService.ShowNotificationInfo("Настройки успешно применены");
            }
        }

        private void LoadReportLayoutView(LoadReportLayoutMessage message)
        {
            switch (message.View)
            {
                case ReportLayoutType.Grid:
                    GridLayout = defaultGridLayout; // Needs to trigger GridLayoutBehavior.OnLayoutChanged()
                    gridControl.FilterString = null;
                    GridLayout = message.Layout;
                    IsAnalyzeMode = false;
                    break;
                case ReportLayoutType.PivotGrid:
                    PivotGridLayout = defaultPivotGridLayout; // Needs to trigger PivotGridLayoutBehavior.OnLayoutChanged()
                    pivotGridControl.FilterString = null;
                    PivotGridLayout = message.Layout;
                    IsAnalyzeMode = true;
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void LoadReportLayoutParameters(LoadReportLayoutMessage message)
        {
            Dictionary<string, ReportLayoutParameter> paramters = message.Parameters.ToDictionary(x => x.Label);

            foreach (ReportParameter reportParameter in Parameters)
            {
                switch (reportParameter.EditorType)
                {
                    case ReportParameterEditorType.DateTimeRange:
                        DateTimeRange dateTimeRange = (DateTimeRange)reportParameter.Value;

                        if (dateTimeRange.PredefinedPeriod == DatePeriodEditValue.None)
                        {
                            dateTimeRange.PredefinedPeriod = DatePeriodEditValue.CurrentMonth;
                        }

                        break;
                    case ReportParameterEditorType.DecimalRange:
                        DecimalRange decimalRange = (DecimalRange)reportParameter.Value;
                        decimalRange.Item1 = null;
                        decimalRange.Item2 = null;
                        break;
                    default:
                        reportParameter.Value = null;
                        break;
                }

                if (paramters.TryGetValue(reportParameter.Label, out ReportLayoutParameter p) && (int)reportParameter.EditorType == p.EditorType)
                {
                    switch (reportParameter.EditorType)
                    {
                        case ReportParameterEditorType.Text:
                            reportParameter.Value = p.Value;
                            break;
                        case ReportParameterEditorType.CheckedTokenComboBox:
                        case ReportParameterEditorType.TokenComboBox:
                            if (p.Value is JArray values)
                            {
                                List<int> editValues = new List<int>();

                                foreach (int value in values)
                                {
                                    if (reportParameter.ComboBoxItems.Any(x => x.Id == value))
                                    {
                                        editValues.Add(value);
                                    }
                                }

                                if (editValues.Any())
                                {
                                    reportParameter.Value = editValues;
                                }
                            }

                            break;
                        case ReportParameterEditorType.DateTimeRange:
                            string period = (string)p.Value;
                            DateTimeRange dateTimeRange = (DateTimeRange)reportParameter.Value;
                            dateTimeRange.PredefinedPeriod = DatePeriodEditValue.GetByName(period);

                            if (dateTimeRange.PredefinedPeriod == DatePeriodEditValue.None)
                            {
                                dateTimeRange.PredefinedPeriod = DatePeriodEditValue.CurrentMonth;
                            }

                            break;
                        case ReportParameterEditorType.DecimalRange:
                            dynamic savedValue = p.Value;
                            DecimalRange decimalRange = (DecimalRange)reportParameter.Value;
                            decimalRange.Item1 = savedValue.Item1;
                            decimalRange.Item2 = savedValue.Item2;
                            break;
                    }
                }
            }
        }

        private IEnumerable<ReportLayoutParameter> GetLayoutParameters()
        {
            if (Parameters == null)
            {
                yield break;
            }

            foreach (ReportParameter p in Parameters)
            {
                object value;

                switch (p.EditorType)
                {
                    case ReportParameterEditorType.DateTimeRange:
                        DateTimeRange range = (DateTimeRange)p.Value;
                        value = range.PredefinedPeriod.Name;
                        break;
                    default:
                        value = p.Value;
                        break;
                }

                yield return new ReportLayoutParameter(p.Label, (int)p.EditorType, value);
            }
        }

        private void Edit()
        {
            Messenger.Send(new ReportViewMessage(reportId));
        }

        private IEnumerable<ReportParameterValueDto> GetParameterValues()
        {
            foreach (ReportParameter parameter in Parameters)
            {
                object value;

                if (parameter.Value is null && parameter.EditorType == ReportParameterEditorType.CheckedTokenComboBox)
                {
                    value = parameter.ComboBoxItems.Select(x => x.Id).ToArray();
                }
                else
                {
                    value = parameter.Value;
                }

                yield return new ReportParameterValueDto { Name = parameter.Label, Value = value };
            }
        }

        public abstract class ValueRange<TValue> : BindableBase
        {
            [JsonProperty("Item1")]
            public TValue Item1
            {
                get
                {
                    return GetProperty(() => Item1);
                }

                set
                {
                    System.Diagnostics.Debug.WriteLine($"SET ValueRange.Item1 {Item1}->{value}");
                    SetProperty(() => Item1, value);
                }
            }

            [JsonProperty("Item2")]
            public TValue Item2
            {
                get
                {
                    return GetProperty(() => Item2);
                }

                set
                {
                    System.Diagnostics.Debug.WriteLine($"SET ValueRange.Item2 {Item2}->{value}");
                    SetProperty(() => Item2, value);
                }
            }
        }

        public sealed class DateTimeRange : ValueRange<DateTime?>
        {
            public DateTimeRange()
            {
                PredefinedPeriod = DatePeriodEditValue.Empty;
            }

            public DatePeriodEditValue PredefinedPeriod
            {
                get { return GetProperty(() => PredefinedPeriod); }
                set { SetProperty(() => PredefinedPeriod, value); }
            }
        }

        private void ShowValidationResultView(string title, IEnumerable<ValidationResultItem> validationItems)
        {
            ValidationResultItem[] validationItemsArray = validationItems as ValidationResultItem[] ?? validationItems.ToArray();

            ValidationResultViewModel viewModel = SizeableDialogDocumentManagerService.ShowView<ValidationResultViewModel>(
                new ValidationResultViewModelParameter(title, validationItemsArray),
                this);

            string validationItemsString = string.Join(", ", validationItemsArray.Select(x => x.Message));

            Logger.LogInformation("ValidationItems: {validationItems}", validationItemsString);
        }

        public sealed class DecimalRange : ValueRange<decimal?>
        {
        }
    }
}