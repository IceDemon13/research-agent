using System.Collections;
using DevExpress.Mvvm;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.PivotGrid;
using Telemart.Client.ViewModels.Reporting.ViewItems;

namespace Telemart.Client.Views.Reporting
{
    /// <summary>
    /// Interaction logic for ReportEditorView.xaml.
    /// </summary>
    public partial class ReportEditorView
    {
        public ReportEditorView()
        {
            InitializeComponent();
        }

        private void FieldsTableViewOnInitNewRow(object sender, InitNewRowEventArgs e)
        {
            FieldsGridControl.SetCellValue(e.RowHandle, AreaColumn, FieldArea.FilterArea.ToString("G"));
            FieldsGridControl.SetCellValue(e.RowHandle, SummaryTypeColumn, FieldSummaryType.Count.ToString("G"));
        }

        private void TableViewOnValidateRow(object sender, GridRowValidationEventArgs e)
        {
            if (e.Row != null)
            {
                e.IsValid = !IDataErrorInfoHelper.HasErrors(e.Row);
            }
        }

        private void TableViewOnInvalidRowException(object sender, InvalidRowExceptionEventArgs e)
        {
            e.ExceptionMode = ExceptionMode.Ignore;
        }

        private void GridColumn_Validate(object sender, GridCellValidationEventArgs e)
        {
            ColumnBase column = e.Column;

            string value = e.Value?.ToString();

            if (string.IsNullOrWhiteSpace(value) || column.FieldName != nameof(ReportFieldViewItem.UniqueName))
            {
                return;
            }

            GridControl grid = (GridControl)column.View.DataControl;
            IList dataSource = grid.ItemsSource as IList;
            if (dataSource == null)
            {
                return;
            }

            for (int i = 0; i < dataSource.Count; i++)
            {
                int rowHandle = grid.GetRowHandleByListIndex(i);

                if (rowHandle == e.RowHandle)
                {
                    continue;
                }

                string cellValue = grid.GetCellValue(rowHandle, nameof(ReportFieldViewItem.UniqueName))?.ToString();
                if (!string.IsNullOrWhiteSpace(cellValue) && cellValue == value)
                {
                    e.IsValid = false;
                    e.ErrorContent = "Название должно быть уникальным";
                    e.Handled = true;
                    return;
                }
            }
        }

        private void TableView_CellValueChanged(object sender, CellValueChangedEventArgs e)
        {
            FieldsGridControl.RefreshData();
        }
    }
}
